この記事は[C# Advent Calendar 2025](https://qiita.com/advent-calendar/2025/csharplang)の25日目の記事です。
[前日はねのさんのDATASに関する紹介記事でした！](https://blog.neno.dev/entry/2025/12/24/130946)

> Premature optimization is the root of all evil

# 概要

- dotnet/runtimeの`Path.RemoveSegments`（絶対パス化せずに . / .. を除去する API）は、2018年に実装案まで完成していたが、Unix環境で既に正規化済みの長いパスに対して顕著な性能劣化を起こし、長年塩漬けになっていた。
- 旧実装は文字列の順方向走査＋temp charバッファ操作であり、最悪$O(N^2)$となる設計だった。
- dotnet/runtimeに対して筆者が提案した新実装では、文字列を逆方向に走査する$O(N)$アルゴリズムに刷新し、さらに SIMDとbit maskを導入することで大幅な高速化を実現した。
- 新実装の最適化の詳細とそれに対するdotnet/runtimeの反応を本記事では記載する。

> Programmers waste enormous amounts of time thinking about, or worrying about, the speed of noncritical parts of their programs

# 背景

私生活が落ち着いてきましたので、数年ぶりに表立って何か公益に資する活動やりてえ～という欲求が高まってきました。
[.NET RuntimeのIssue欄](https://github.com/dotnet/runtime/issues)には膨大な数のIssueが存在しており、各Issueにはラベルが割り当てられています。
何か貢献したい方は`help wanted`と`api-approved`ラベルの付与されたIssueが特に狙い目です。
`help wanted`は「外部コントリビューター歓迎」を意味していますし、`api-approved`は「実装されたら受け入れる」という意味なので無駄骨になる可能性が低いですからね。
完全新規の提案をすると放置された上に自転車置き場の議論が発生しがちです。

今回は[Path.RemoveSegments](https://github.com/dotnet/runtime/issues/2162)に関して実装していきましょう。

https://github.com/dotnet/runtime/issues/2162

## Path.RemoveSegmentsとは

**絶対パスにせずにパスの正規化を行う関数です。**
ファイルIOをせずにパスの同一性判定を高速に行いたいことがあったりしませんか？
その際に考慮すべき点として、パスには`.`や`..`という相対パス要素が含まれるという事実があります。
`/a/./b/unused/../c` 左記パスを正規化すると`/a/b/c`が得られます。
これまでこの正規化を行う際には[`Path.GetFullPath`関数](https://learn.microsoft.com/ja-jp/dotnet/api/system.io.path.getfullpath?view=net-10.0)が使用されてきました。
ただ、これは戻り値が絶対パスとなるため絶妙に使い辛いものでした。
`Path.RemoveSegments`は2018年6月28日に提案され、実装素案もPull Requestとして提出されテストも完備されていました。しかしUnix環境において`/a/b/c`のような正規化済みの完全パスに対して`Path.GetFullPath`を実行した場合と比較して明らかな性能の劣化が認められてしまいました。
当時のベンチマーク結果は見当たりませんでしたがGitHub Actionsで検証したところ、以下のような結果となりました。

```
BenchmarkDotNet v0.15.2, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.100
  [Host]  : .NET 10.0.0 (10.0.25.52411), X64 RyuJIT AVX2
  LongRun : .NET 10.0.0 (10.0.25.52411), X64 RyuJIT AVX2

Job=LongRun  IterationCount=100  LaunchCount=3  
WarmupCount=15  
```

| Method      | Source               | Median     | Mean       | Error     |
|------------ |--------------------- |-----------:|-----------:|----------:|
| #2162       | /                    |  13.006 ns |  13.058 ns | 0.0350 ns |
| GetFullPath | /                    |   8.956 ns |   8.956 ns | 0.0011 ns |
|             |                      |            |            |           |
| #2162       | /some(...)ments [45] | 110.283 ns | 110.427 ns | 0.1792 ns |
| GetFullPath | /some(...)ments [45] |  74.762 ns |  74.944 ns | 0.1034 ns |
|             |                      |            |            |           |
| #2162       | /som(...)piyo [122]  | 277.733 ns | 277.949 ns | 0.4333 ns |
| GetFullPath | /som(...)piyo [122]  | 199.247 ns | 199.276 ns | 0.0567 ns |
|             |                      |            |            |           |
| #2162       | /som(...)ers/ [216]  | 489.890 ns | 490.205 ns | 0.6612 ns |
| GetFullPath | /som(...)ers/ [216]  | 350.432 ns | 350.914 ns | 0.2984 ns |

約40%の性能劣化ですね。
かくしてPath.RemoveSegmentsは塩漬けされることとなりました。

> These attempts at efficiency actually have a strong negative impact when debugging and maintenance are considered

# 環境

## ローカル開発環境

AMD Ryzen 7 8840U
RAM 16GB
X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
.NET 10

## CI環境

GitHub Actions Ubuntu 24
.NET 10

## リポジトリ

https://github.com/pCYSl5EDgo/Pcysl5edgo.RemoveRedundantPath

# 方法

以下記載のルールを満たすように実装していきます。

## Path正規化のルール

[オリジナルのPRに掲載されたルール](https://github.com/dotnet/runtime/pull/37939)を以下に和訳します。

### Unix

- パス冒頭の`/`はルートを意味します。また、これはセグメント間のセパレータでもあります。繰り返し連続しているセパレータは融合して1つとして扱われます。[^1]
- `.`はカレントディレクトリを意味します。既知のルートが存在しない場合に冒頭になければ除外されます。
- `..`は以前のディレクトリに辿って戻ります(バックトラック)。直前のセグメントは除外され、当該`..`セグメントも追加されません。例外として、既知のルートが存在せず、かつ辿るべき以前のディレクトリがないかあるいは`..`の連続しかないならば、追加してよいです。
- `\`はファイルやディレクトリの名前として有効な文字です。
- 3つ以上の`.`のみからなるセグメントは有効な名前です。

### Windows

- `\`も`/`と同様にセパレータとみなされます。なお、`/`は正規化されると`\`になります。
- セパレータ、`.`、`..`についてはUnixのルールとほぼ同様です。
- 通常3つ以上の`.`のみからなるセグメントは有効な名前です。なお、末尾にセパレータを伴わない状態で最後のセグメントになった場合は除外されます。
- 1つ以上の`.`を末尾に持つセグメントは末尾の連続する`.`を除外されます。
- デバイスプレフィックス(`\\.\` `\\?\` `\??\`)で始まるパスには特殊なルールが適用されます。
  - 3つ以上の`.`のみからなるセグメントは除外されません。
  - 末尾の連続する`.`は除外されません。
- ドライブ名の後にセパレータがなくても有効ではあります。無資格(unqualified)とみなされます。
- 冒頭セパレータで始まり、かつドライブ名を持たないパスはルートを持つとみなされます。

なお上記ルールではテストケースをパスできないことが後に実装していて判明しました。
私はテスト駆動開発を全肯定する立場にはないのですが、Windows版についてはテスト駆動開発せざるを得ませんでした。
詳細なルールに関してはWindows版の実装を読めばわかると思います。

## オリジナル実装の問題点

順方向に一文字ずつ読み取り、`List<char>`相当の一時バッファに書き込みを行っていました。`..`のバックトラックでは一時バッファを末尾から読み取って削除すべき最後のセグメントを同定するなどしており無駄にメモリを読み書きしていました。この計算量は最悪$O(N^2)$となりえます。

入力: `../../a/b/c/d/../../../../../e`
期待される出力: `../../../e`

オリジナル実装での各ステップの挙動
入力: `../../a/b/c/d/../../../../../e`
- ステップ1: `../`
  - 一時バッファ: `../`
- ステップ2: `../`
  - 一時バッファ末尾がペアレントセグメントか確認しペアレントセグメントなので一時バッファに追記
  - 一時バッファ: `../../`
- ステップ3: `a/`
  - 通常のセグメントなので一時バッファに追記
  - 一時バッファ: `../../a/`
- ステップ4-6: `b/c/d/`
　- 一時バッファ: `../../a/b/c/d/`
- ステップ7: `../`
  - 一時バッファ末尾がペアレントセグメントか確認しペアレントセグメントではないのでセパレータが出てくるまで末尾を削除
  - 一時バッファ: `../../a/b/c/`
- ステップ8-10: `../../../`
  - 一時バッファ: `../../`
- ステップ11: `../`
  - 一時バッファ末尾がペアレントセグメントか確認しペアレントセグメントなので一時バッファに追記
  - 一時バッファ: `../../../`
- ステップ12: `e`
  - 通常のセグメントなので一時バッファに追記
  - 一時バッファ: `../../../e`
- ステップ13: ToString()

ペアレントセグメントに出会う度に一時バッファを逆順に走査して末尾のセグメントがペアレントセグメントであるか否かを判定しています。
極端な例ではありますが、`/超長い文字列/../超長い文字列/../超長い文字列/../超長い文字列/..`のような入力だと無駄に右往左往することになります。

> We should forget about small efficiencies, say about 97% of the time

# 改善

## アルゴリズム

最初と最後の1文字が`/`であるか否かだけは最初に調べる必要こそありますが、残りの中間部分を逆方向に走査すれば$O(N)$で正規化可能です。

```
/ a / b / c / ..
```

後ろから`..`を読み取る度にカウンターとして用意した変数をインクリメントします。通常のセグメントが出現した際に`..`のカウンターが0ならばFILOなセグメントのスタックにプッシュします。カウンターが非0の場合デクリメントすることで相殺します。
セグメントのスタックを`/`で連結すれば正規化されたパスを得ることができます。
`.`や`/`で始まる場合の扱いに関しては条件分けすれば十分扱えますので詳述しませんが、気になる方はご自分で実装してみるのも面白いでしょう。

[2022年にiSazanov氏が逆方向走査を提案していました](https://github.com/dotnet/runtime/issues/2162#issuecomment-1285453514)が、なぜか具体的にアルゴリズムを説明せず、実装もまたしていませんでした。

以下のコードは最もシンプルなUnix用の正規化メソッドの中核部分です。

```csharp
public int InitializeEach(int textLength)
{
    // 正の数: 通常のセグメント
    // 負の数: .の連続しているセグメント。-2まで。-3以下は存在せず3という正の数になります。
    int mode = 0;
    // セグメントに含まれている文字数です。関数内で累積を管理する方が高速でした。
    int segmentCharCount = 0;
    // 後ろから逆順に辿ります。
    for (int textIndex = textLength - 1; textIndex >= 0; --textIndex)
    {
        // ReadOnlySpan<char>に対する単なるインデックスアクセスに相当します。
        var c = Unsafe.Add(ref textRef, textIndex);
        if (mode > 0)
        {
            if (c != '/')
            {
                ++mode;
                continue;
            }

            // 通常のセグメントでセパレータに出会った時
            if (parentSegmentCount != 0)
            {
                --parentSegmentCount;
            }
            else
            {
                // セグメントのスタックにセグメントを登録します。
                // 直前のセグメントと離れていなければセグメントを融合させます。
                // 離れているセグメントとは abc//__/../def におけるabcとdefのようなものです。
                segmentCharCount += AddOrUniteSegment(textIndex + 1, mode, textIndex + mode);
            }

            mode = 0;
        }
        else if (mode == 0)
        {
            // 初期モードではセパレータに出会っても何もせず無視します。
            if (c != '/')
            {
                mode = c == '.' ? -1 : 1;
            }
        }
        else if (mode == -1)
        {
            if (c == '/')
            {
                // ./..の場合、.を消し飛ばして..となるべきです。
                // しかし、__/../.の場合、.は残すべきです。
                // ././.とあっても.になるべきですので、.の有無はboolで管理するのが効率的です。
                hasLeadingCurrentSegment = parentSegmentCount == 0;
                mode = 0;
            }
            else
            {
                mode = c == '.' ? -2 : 2;
            }
        }
        else
        {
            Debug.Assert(mode == -2);
            if (c == '/')
            {
                ++parentSegmentCount;
                mode = 0;
            }
            else
            {
                mode = 3;
            }
        }
    }

    // forループを抜けた後に残ったセグメントを処理します。
    // abc/defのようにセパレータで始まらないパスがここで処理されます。
    if (mode > 0)
    {
        if (parentSegmentCount > 0)
        {
            --parentSegmentCount;
        }
        else
        {
            segmentCharCount += AddOrUniteSegment(0, mode, mode + 1);
        }
    }
    else if (!startsWithSeparator)
    {
        if (mode == -1)
        {
            hasLeadingCurrentSegment = true;
        }
        else
        {
            Debug.Assert(mode == -2);
            ++parentSegmentCount;
        }
    }

    return CalculateLength(segmentCharCount);
}
```


## SIMD

前節までのコードは実質的に`.`と`/`とそれ以外の3値に対して逆順走査しているわけです。
ならばSIMDを利用してパスの文字列を高速に3値に分類すれば性能向上するのでは？という発想が生まれます。

実際3値の`enum array`にするよりは`uint Get(ReadOnlySpan<char> source, out uint dot)`という風に4byte×2の`bit mask`に32文字(64byte)分のデータを集約した方が取り回しが良かったのでそうしました。

`uint`型変数`dot`は`n`文字目が`.`であれば`n`bit目も`1`となり、そうでなければ`0`となるような整数です。`<<`や`>>`などのシフトと組み合わせた`((dot>>n)&1)!=0`という`bit test`によって容易に`n`文字目が`.`か否か知ることが可能となります。こういうものをこの記事では`bit mask`と呼びましょう。
`bit mask`にすることでメモリもコンパクトになり、更に32/64文字分を一気に走査することもできたりしますので嬉しいことが多々あります。

以後`Vector128/256/512<T>`を総称して`VectorX<T>`と記載します。

`VectorX<T>.Equals(VectorX<T>, VectorX<T>)`関数は各T型の要素を比較し、真ならば全ビット1(符号付整数の場合は-1)、偽ならば0を戻り値の各要素に格納します。
`VectorX.LoadUnsafe(ref ushort)`で得た`VectorX<ushort>`型ベクトル変数と`VectorX.Create(ushort)`で作成した`.`と`/`で一様に初期化されたベクトル変数を`Equals`で比較すれば一気に8/16/32文字分の比較が可能なのです。
とはいえ比較結果が`VectorX<ushort>`型だと取り回しが悪いので`uint`型に変換します。この際`VectorX<byte>.ExtractMostSignificantBits`関数が極めて便利です。これは各`byte`要素の最上位bitを抽出して`uint`/`ulong`型の戻り値に取りまとめてくれます。
これは少し過言ですが元の`char`は16bitですのでそれが1bitになれば16倍情報の密度が上がり、かつ取り扱いも楽になります。いやまあここまで下拵えが必要な時点で楽と言ったら語弊はありますが……

エンディアンは今回考慮しなくて良いでしょう。.NET10が動くプロセッサでBigEndianかつSIMD使用可能なプロセッサは調べた限りARM位ですし、今回の書き方だとARMの場合特に問題は生じないようですので。

```csharp
public static uint Get(ReadOnlySpan<char> source, out uint dot)
{
    Debug.Assert(source.Length >= 32);
    return Get(ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(source)), out dot);
}

private static uint Get(ref ushort source, out uint dot)
{
    // VectorXがサポートされている場合は分岐命令が存在しないのでパイプラインがストールすることはないです。
    if (Vector512.IsHardwareAccelerated)
    {
        var v = Vector512.LoadUnsafe(ref source);
        var compound = Vector512.Narrow(Vector512.Equals(v, Vector512.Create((ushort)'.')), Vector512.Equals(v, Vector512.Create((ushort)'/'))).ExtractMostSignificantBits();
        dot = (uint)compound;
        return (uint)(compound >>> 32);
    }
    else if (Vector256.IsHardwareAccelerated)
    {
        var v0 = Vector256.LoadUnsafe(ref source);
        var v1 = Vector256.LoadUnsafe(ref source, 16);
        var d = Vector256.Create((ushort)'.');
        dot = Vector256.Narrow(Vector256.Equals(v0, d), Vector256.Equals(v1, d)).ExtractMostSignificantBits();
        var s = Vector256.Create((ushort)'/');
        return Vector256.Narrow(Vector256.Equals(v0, s), Vector256.Equals(v1, s)).ExtractMostSignificantBits();
    }
    else if (Vector128.IsHardwareAccelerated)
    {
        // 32文字分を4回で読み取ります。
        // ページキャッシュアクセスは細切れにするより一気にした方が良い気がします。
        var v0 = Vector128.LoadUnsafe(ref source);
        var v1 = Vector128.LoadUnsafe(ref source, 8);
        var v2 = Vector128.LoadUnsafe(ref source, 16);
        var v3 = Vector128.LoadUnsafe(ref source, 24);
        // ここdという変数に切り出さない方が良い時代(.NET7頃)がありました。
        // さすがにくどいので変数に取り纏めています。
        var d = Vector128.Create((ushort)'.');
        var d0 = Vector128.Narrow(Vector128.Equals(v0, d), Vector128.Equals(v1, d)).ExtractMostSignificantBits();
        var d1 = Vector128.Narrow(Vector128.Equals(v2, d), Vector128.Equals(v3, d)).ExtractMostSignificantBits();
        dot = d0 | (d1 << 16);
        var s = Vector128.Create((ushort)'/');
        var s0 = Vector128.Narrow(Vector128.Equals(v0, s), Vector128.Equals(v1, s)).ExtractMostSignificantBits();
        var s1 = Vector128.Narrow(Vector128.Equals(v2, s), Vector128.Equals(v3, s)).ExtractMostSignificantBits();
        return s0 | (s1 << 16);
    }
    else
    {
        // フォールバック
        uint _separator = default, _dot = default;
        for (int i = 0; i < 32; ++i)
        {
            switch (Unsafe.Add(ref source, i))
            {
                case '.':
                    _dot |= 1u << i;
                    break;
                case '/':
                    _separator |= 1u << i;
                    break;
            }
        }

        dot = _dot;
        return _separator;
    }
}
```

## セグメントスタックの実装

セグメントは`(int Offset, int Length)`という`ValueTuple`で表現することにしました。
そして初期はstackallocで確保した小さなスタックで扱い、溢れたら`ArrayPool<long>`で再確保したスタック領域にコピーすることにしました。
当初の実装では文字列を最初に走査してseparatorとdotのペアを配列にため込み、最適な大きさのセグメントスタックを確保しようとしていました。ですが、この場合`n`文字の入力(`n*2`バイト)に対して(`n*4*2/32 == n/4`バイトの)separator+dot配列のアロケーションがそれなりに大きすぎました。
連続するseparatorがない場合`Sum(BitOperations.PopCount(separator))+1`で正確にセグメント数が表現されます。連続するseparatorがあってもセグメント数が減るだけなのでセグメントスタックのキャパシティを計るのに困ることはありません。
しかし、現にそうなっていないのはAVX2でSIMDを利用して`VectorX<uint>`に対してpopcountするのというのが簡単ではないからです。[先行研究は2016年頃に存在していますけれども](https://www.researchgate.net/publication/386694228_Faster_Population_Counts_Using_AVX2_Instructions)。
セグメントスタックのキャパシティ上限を正確に割り出すためだけにfor文回してpopcountするのは非効率な気がしまして全体のアロケーションを減らすことも兼ねてseparatorとdotの配列を事前確保せずに都度都度計算することといたしました。
しかしやはり計測しないで憶測で実装するのも気持ち悪いので、後に色々アロケーションの条件を変えてみたりしました。文字数が1024文字を超えてくると明確に最初にセグメントの`Span`を確保する方が早いですね。

さて、セグメントを上手に扱おうと考える時、セグメントの切れ目となるものは何か考えてみましょう。
`/`(セパレータ)？
その通りですが、そこで留まるのは惜しいですね。`abc` `/` `def`という2つのセグメントは(ペアレントセグメントが関与しなければ)実質的に`abc/def`という1つのセグメントとして扱うことができます。
そしてセグメントが融合して少なくなればなるほど最終的に`ToString`なり`CopyTo(Span<char>)`なりでセグメントスタックに対して回す`foreach`が短くなってメモリコピーが高速化されます。

```csharp
private Span<(int Offset, int Length)> segmentSpan;
private int segmentCount;

private void AddSegment(int offset, int length)
{
    // セグメントスタック容量が不足していればArrayPoolで必要分確保して既存セグメントをコピーします。
    if (++segmentCount > segmentSpan.Length)
    {
        if (rentalArray is null)
        {
            // ArrayPool<ValueTuple<int, int>>ではなくArrayPool<long>を使用している理由はジェネリクスのTにstruct型を使うとJIT後の機械語サイズが肥大するので、他の箇所で使用されている型を再利用する方が良さそうだからです。
            rentalArray = ArrayPool<long>.Shared.Rent((int)BitOperations.RoundUpToPowerOf2((uint)segmentCount));
            var temp = MemoryMarshal.Cast<long, ValueTuple<int, int>>(rentalArray.AsSpan());
            segmentSpan.CopyTo(temp);
            segmentSpan = temp;
        }
        else
        {
            var tempRentalArray = ArrayPool<long>.Shared.Rent((int)BitOperations.RoundUpToPowerOf2((uint)segmentCount));
            var temp = MemoryMarshal.Cast<long, ValueTuple<int, int>>(tempRentalArray.AsSpan());
            segmentSpan.CopyTo(temp);
            ArrayPool<long>.Shared.Return(rentalArray);
            rentalArray = tempRentalArray;
            segmentSpan = temp;
        }
    }

    segmentSpan[segmentCount - 1] = new(offset, length);
}

// 最新セグメントが存在し、かつそのセグメントのoffsetが隣接セグメントとして想定されるoffsetの場合に最新セグメントの長さを更新します。
// Unixの場合は区切り文字が1種類しかないからセグメント融合判定は楽です。
// Windows? 区切り文字バックスラッシュだけに限定していればよかったものを……
private int AddOrUniteSegment(int offset, int length, int expectedOffset)
{
    hasLeadingCurrentSegment = false;
    if (segmentCount > 0)
    {
        ref var last = ref segmentSpan[segmentCount - 1];
        if (last.Offset == expectedOffset)
        {
            last.Offset = offset;
            last.Length += ++length;
            return length;
        }
    }

    AddSegment(offset, length);
    return length;
}
```

## 32要素以下のSIMDコード

32要素なのは`uint`型が32bitだからです。`ulong`型の実装もしてみましたのでそちらの方では64要素を境界として関数を別に作っています。

セグメントを融合させることの利点は前述しました。
逆順走査している時から既に融合したセグメントを扱えると嬉しいですよね。
セパレータ`/`を`LastIndexOf`するような方法だと細切れのセグメントしか扱えません。
カレントセグメント(`current`)、ペアレントセグメント(`parent`)、連続したセパレータ(`separatorDuplicate`)の3種類のbit maskを使用することで確実にセグメントが融合しない`index`がわかります。

- `a/../b`
- `a/./b`
- `a//b`

上記のいずれも`a`と`b`のセグメントは融合しえませんよね？`..`の場合はそもそも`a`が消滅しますけれども。

```csharp
private int InitializeSimdLTE32()
{
    // 本当は宣言と初期化は同時にすべきですが、64bit版へのコピペビリティを上げるためにこのような記述法となっています。
    const uint OneBit = 1u;
#pragma warning disable IDE0018
    uint separator, dot, separatorWall, current, parent, separatorDuplicate;
#pragma warning restore IDE0018
    if (textSpan.Length == 32)
    {
        separator = BitSpan.Get(textSpan, out dot);
    }
    else
    {
        separator = BitSpan.Get(textSpan, out dot, textSpan.Length);
    }

    // 番兵を配置するとカレントセグメント.とペアレントセグメント..を計算するのが楽になります。
    separatorWall = BitSpan.CalculateSeparatorWall(separator, textSpan.Length - 1);
    // カレントセグメントは/./の並びという意味です。
    current = dot & ((separator << 1) | OneBit) & separatorWall;
    // ペアレントセグメントは/../の並びという意味です。
    // ここで逆順走査をするため、1となるべきbitは..の後の方の.です。
    parent = dot & (dot << 1) & ((separator << 2) | (OneBit << 1)) & separatorWall;
    // ///のようにセパレータが連続して並んでいる場合に最後の1つのセパレータを除いてbitが立ちます。
    separatorDuplicate = separator & separatorWall;
    if ((current | parent | separatorDuplicate) == 0)
    {
        // カレントセグメントもペアレントセグメントもセパレータも連続しなければ、正規化済みのパスとして扱えます。
        return textSpan.Length + (startsWithSeparator ? 1 : 0) + (endsWithSeparator ? 1 : 0);
    }

    int segmentCharCount = 0;
    var textIndex = textSpan.Length - 1;
    var continueLength = ProcessLoop(ref segmentCharCount, ref textIndex, 0, separator, separatorDuplicate, current, parent, 0);
    if (continueLength > 0)
    {
        segmentCharCount = ProcessLastContinuation(segmentCharCount, continueLength);
    }

    return CalculateLength(segmentCharCount);
}
```

`ProcessLoop`と`ProcessLastContinuation`、そして`CalculateLength`関数に非正規化済パスの時の実質的処理を委譲しています。
32要素より多い場合と共通している部分を切り出した結果このような書き方になりました。

## 32要素より多い場合のSIMDコード

```csharp
private int InitializeSimdGT32()
{
    const int BitShift = 5;
    const int BitCount = 1 << BitShift;
    Debug.Assert(textSpan.Length >= BitCount + 1);
    const int BitMask = BitCount - 1;
    const uint OneBit = 1u;
#pragma warning disable IDE0018
    uint separatorCurrent, separatorPrev, dotCurrent, dotPrev, separatorWall, current, parent, separatorDuplicate;
#pragma warning restore IDE0018
    int segmentCharCount = 0, textIndex = textSpan.Length - 1, batchCount = (textSpan.Length + BitMask) >>> BitShift, batchIndex = batchCount - 2;
    if ((textSpan.Length & BitMask) == default)
    {
        separatorCurrent = BitSpan.Get(ref Unsafe.As<char, ushort>(ref Unsafe.Add(ref MemoryMarshal.GetReference(textSpan), batchIndex * BitCount + BitCount)), out dotCurrent);
    }
    else
    {
        separatorCurrent = BitSpan.Get(ref Unsafe.As<char, ushort>(ref Unsafe.Add(ref MemoryMarshal.GetReference(textSpan), batchIndex * BitCount + BitCount)), out dotCurrent, textSpan.Length & BitMask);
    }

    separatorPrev = BitSpan.Get(ref Unsafe.As<char, ushort>(ref Unsafe.Add(ref MemoryMarshal.GetReference(textSpan), batchIndex * BitCount)), out dotPrev);
    separatorWall = BitSpan.CalculateSeparatorWall(separatorCurrent, textSpan.Length - 1);
    current = dotCurrent & ((separatorCurrent << 1) | (separatorPrev >>> BitMask)) & separatorWall;
    parent = dotCurrent & ((dotCurrent << 1) | (dotPrev >>> BitMask)) & ((separatorCurrent << 2) | (separatorPrev >>> (BitCount - 2))) & separatorWall;
    separatorDuplicate = separatorCurrent & separatorWall;
    var continueLength = ProcessLoop(ref segmentCharCount, ref textIndex, 0, separatorCurrent, separatorDuplicate, current, parent, batchIndex + 1);
    while (--batchIndex >= 0)
    {
        separatorWall = (separatorCurrent << BitMask) | (separatorPrev >>> 1);
        separatorCurrent = separatorPrev;
        dotCurrent = dotPrev;
        separatorPrev = BitSpan.Get(ref Unsafe.As<char, ushort>(ref Unsafe.Add(ref MemoryMarshal.GetReference(textSpan), batchIndex * BitCount)), out dotPrev);
        current = dotCurrent & ((separatorCurrent << 1) | (separatorPrev >>> BitMask)) & separatorWall;
        parent = dotCurrent & ((dotCurrent << 1) | (dotPrev >>> BitMask)) & ((separatorCurrent << 2) | (separatorPrev >>> (BitCount - 2))) & separatorWall;
        separatorDuplicate = separatorCurrent & separatorWall;
        continueLength = ProcessLoop(ref segmentCharCount, ref textIndex, continueLength, separatorCurrent, separatorDuplicate, current, parent, batchIndex + 1);
    }

    separatorWall = (separatorCurrent << BitMask) | (separatorPrev >>> 1);
    current = dotPrev & ((separatorPrev << 1) | OneBit) & separatorWall;
    parent = dotPrev & (dotPrev << 1) & ((separatorPrev << 2) | (OneBit << 1)) & separatorWall;
    separatorDuplicate = separatorPrev & separatorWall;
    continueLength = ProcessLoop(ref segmentCharCount, ref textIndex, continueLength, separatorPrev, separatorDuplicate, current, parent, 0);
    if (continueLength > 0)
    {
        segmentCharCount = ProcessLastContinuation(segmentCharCount, continueLength);
    }

    return CalculateLength(segmentCharCount);
}
```

`uint`型1つに収まらない場合、2つ並べて計算する必要があります。境界部分に`./`があった場合、1つ前の`uint`の末尾が`/`なのか`/.`なのかあるいはそれ以外かということがカレントセグメントなのかペアレントセグメントなのか、あるいは普通のセグメントなのかを決定づけます。
故に`current`と`prev`と接尾辞を付与したローカル変数を2つで1組として運用する必要があるのです。

## ProcessLoop

出現するセグメントに応じて処理を行っています。
特に通常のセグメントである場合は`BitOperations.LeadingZeroCount`を利用して次の`current`, `parent`, `separatorDuplicate`の立っている`bit flag`を求めています。

```csharp
private int ProcessLoop(ref int segmentCharCount, ref int textIndex, int continueLength, uint separator, uint separatorDuplicate, uint current, uint parent, int batchIndex)
{
    // ulong型だとBitCountは64になります。
    const int BitCount = 32, BitMask = BitCount - 1;
    var loopLowerLimit = batchIndex * BitCount;
    var loopUpperLimit = loopLowerLimit + BitCount;
    int nextSeparatorIndex, length;
    #region ContinueLength > 0
    // continueLengthとは前までのbit maskの端切れの長さです。
    if (continueLength > 0)
    {
        // 端切れの位置がセパレータか否かで挙動が変わります。
        if (BitSpan.GetBit(separator, textIndex))
        {
            nextSeparatorIndex = textIndex;
            length = continueLength;
        }
        else
        {
            Debug.Assert(!BitSpan.GetBit(parent, textIndex) && !BitSpan.GetBit(current, textIndex));
            var temp = BitSpan.ZeroHighBits(separator, textIndex);
            nextSeparatorIndex = loopUpperLimit - 1 - BitOperations.LeadingZeroCount(temp);
            length = textIndex - nextSeparatorIndex + continueLength;
        }

        if (nextSeparatorIndex < loopLowerLimit)
        {
            textIndex = nextSeparatorIndex;
            Debug.Assert(length >= 0);
            return length;
        }
        else if (parentSegmentCount > 0)
        {
            --parentSegmentCount;
        }
        else
        {
            hasLeadingCurrentSegment = false;
            if (segmentCount == 0)
            {
                AddSegment(nextSeparatorIndex + 1, segmentCharCount = length);
            }
            else
            {
                // ここだけ特殊なセグメントの融合の仕方をします。
                ref var oldPair = ref LastSegment;
                var diff = oldPair.Offset - nextSeparatorIndex - length - 1;
                switch (diff)
                {
                    // abc 切れ目 def
                    // abc 切れ目 /def
                    // のような場合をこうすることで上手く扱えます。
                    case 0:
                    case 1:
                        oldPair.Offset = nextSeparatorIndex + 1;
                        oldPair.Length += (length += diff);
                        break;
                    default:
                        AddSegment(nextSeparatorIndex + 1, length);
                        break;
                }

                segmentCharCount += length;
            }
        }

        textIndex = nextSeparatorIndex - 1;
        if (textIndex < loopLowerLimit)
        {
            return 0;
        }
    }
    else
    {
        Debug.Assert(continueLength == 0, $"{nameof(continueLength)}: {continueLength}");
    }
    #endregion

    do
    {
        if ((textIndex & BitMask) != BitMask)
        {
            var clearStartIndex = textIndex + 1;
            separator = BitSpan.ZeroHighBits(separator, clearStartIndex);
            separatorDuplicate = BitSpan.ZeroHighBits(separatorDuplicate, clearStartIndex);
            parent = BitSpan.ZeroHighBits(parent, clearStartIndex);
            current = BitSpan.ZeroHighBits(current, clearStartIndex);
        }

        var any = parent | current | separatorDuplicate;
        if (any == 0)
        {
            if (parentSegmentCount == 0)
            {
                continueLength = (textIndex & BitMask) + 1;
                textIndex = loopLowerLimit - 1;
                Debug.Assert(continueLength >= 0);
                return continueLength;
            }
            else
            {
                // ペアレントセグメントを今含まれているセグメントの数で打ち消しあいます。
                parentSegmentCount -= BitOperations.PopCount(BitSpan.ZeroHighBits(separator, textIndex));
                if (parentSegmentCount >= 0)
                {
                    textIndex = loopLowerLimit - 1;
                    return BitOperations.TrailingZeroCount(separator);
                }
                else
                {
                    // セグメントの数の方が多ければ適切なtextIndex位置に復元します。
                    var tempSeparator = separator;
                    for (; parentSegmentCount < 0; ++parentSegmentCount, tempSeparator = BitSpan.ResetLowestSetBit(tempSeparator))
                    {
                    }

                    textIndex = loopLowerLimit - 1;
                    return BitOperations.TrailingZeroCount(tempSeparator);
                }
            }
        }
        else if (BitSpan.GetBit(any, textIndex))
        {
            if (BitSpan.GetBit(separatorDuplicate, textIndex))
            {
                var temp = BitSpan.ZeroHighBits(~separatorDuplicate, textIndex);
                textIndex = loopUpperLimit - 1 - BitOperations.LeadingZeroCount(temp);
            }
            else if (BitSpan.GetBit(parent, textIndex))
            {
                ++parentSegmentCount;
                textIndex -= 3;
            }
            else
            {
                Debug.Assert(BitSpan.GetBit(current, textIndex));
                hasLeadingCurrentSegment = parentSegmentCount == 0;
                textIndex -= 2;
            }

            continue;
        }
        else if (parentSegmentCount > 0)
        {
            {
                var temp = BitSpan.ZeroHighBits(separator, textIndex);
                nextSeparatorIndex = loopUpperLimit - 1 - BitOperations.LeadingZeroCount(temp);
                length = textIndex - nextSeparatorIndex;
            }
            if (nextSeparatorIndex >= loopLowerLimit)
            {
                --parentSegmentCount;
                textIndex = nextSeparatorIndex - 1;
                continue;
            }
        }
        else
        {
            var temp = BitSpan.ZeroHighBits(any, textIndex);
            nextSeparatorIndex = loopUpperLimit - BitOperations.LeadingZeroCount(temp);
            length = textIndex - nextSeparatorIndex;
            if (nextSeparatorIndex >= loopLowerLimit)
            {
                segmentCharCount += AddOrUniteSegment(nextSeparatorIndex + 1, length, textIndex + 2);
                textIndex = nextSeparatorIndex - 1;
                continue;
            }
        }

        textIndex = nextSeparatorIndex;
        Debug.Assert(length >= 0, $"{nameof(length)}: {length} {nameof(textIndex)}: {textIndex}");
        return length;
    }
    while (textIndex >= loopLowerLimit);
    return 0;
}
```

## BitSpanユーティリティクラスについて

`BitOperations`に不足している機能を実装するためのユーティリティクラスです。

```csharp
public static class BitSpan
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetBit(ulong array, int bitOffset)
    {
        return ((array >>> bitOffset) & 1ul) != default;
    }

    public static string ToString(ref ulong bitArray, int bitLength)
    {
        return string.Create(bitLength, MemoryMarshal.CreateReadOnlySpan(ref bitArray, (bitLength + 63) >>> 6), static (span, bitArraySpan) =>
        {
            for (int i = 0; i < span.Length; ++i)
            {
                span[i] = (bitArraySpan[i >>> 6] & (1ul << (i & 63))) != default ? '1' : '0';
            }
        });
    }

    public static uint Get(ReadOnlySpan<char> source, out uint dot)
    {
        Debug.Assert(source.Length >= 32);
        return Get(ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(source)), out dot);
    }

    public static uint Get(ReadOnlySpan<char> source, out uint dot, int length)
    {
        Debug.Assert(source.Length >= length);
        Debug.Assert(length > 0);
        return Get(ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(source)), out dot, length);
    }

    public static uint Get(ref ushort source, out uint dot)
    {
        if (Vector512.IsHardwareAccelerated)
        {
            var v = Vector512.LoadUnsafe(ref source);
            var compound = Vector512.Narrow(Vector512.Equals(v, Vector512.Create((ushort)'.')), Vector512.Equals(v, Vector512.Create((ushort)'/'))).ExtractMostSignificantBits();
            dot = (uint)compound;
            return (uint)(compound >>> 32);
        }
        else if (Vector256.IsHardwareAccelerated)
        {
            var v0 = Vector256.LoadUnsafe(ref source);
            var v1 = Vector256.LoadUnsafe(ref source, 16);
            var d = Vector256.Create((ushort)'.');
            dot = Vector256.Narrow(Vector256.Equals(v0, d), Vector256.Equals(v1, d)).ExtractMostSignificantBits();
            var s = Vector256.Create((ushort)'/');
            return Vector256.Narrow(Vector256.Equals(v0, s), Vector256.Equals(v1, s)).ExtractMostSignificantBits();
        }
        else if (Vector128.IsHardwareAccelerated)
        {
            var v0 = Vector128.LoadUnsafe(ref source);
            var v1 = Vector128.LoadUnsafe(ref source, 8);
            var v2 = Vector128.LoadUnsafe(ref source, 16);
            var v3 = Vector128.LoadUnsafe(ref source, 24);
            var d = Vector128.Create((ushort)'.');
            var d0 = Vector128.Narrow(Vector128.Equals(v0, d), Vector128.Equals(v1, d)).ExtractMostSignificantBits();
            var d1 = Vector128.Narrow(Vector128.Equals(v2, d), Vector128.Equals(v3, d)).ExtractMostSignificantBits();
            dot = d0 | (d1 << 16);
            var s = Vector128.Create((ushort)'/');
            var s0 = Vector128.Narrow(Vector128.Equals(v0, s), Vector128.Equals(v1, s)).ExtractMostSignificantBits();
            var s1 = Vector128.Narrow(Vector128.Equals(v2, s), Vector128.Equals(v3, s)).ExtractMostSignificantBits();
            return s0 | (s1 << 16);
        }
        else
        {
            uint _separator = default, _dot = default;
            for (int i = 0; i < 32; ++i)
            {
                switch (Unsafe.Add(ref source, i))
                {
                    case '.':
                        _dot |= 1u << i;
                        break;
                    case '/':
                        _separator |= 1u << i;
                        break;
                }
            }

            dot = _dot;
            return _separator;
        }
    }

    public static uint Get(ref ushort source, out uint dot, int length)
    {
        Debug.Assert((uint)(length - 1) < 31u);
        uint separator = 0, _dot = 0;
        int i = 0;
        if (Vector128.IsHardwareAccelerated && length >= 16)
        {
            for (; i + Vector128<ushort>.Count < length; i += Vector128<ushort>.Count)
            {
                var v = Vector128.LoadUnsafe(ref source, (nuint)i);
                var compound = Vector128.Narrow(Vector128.Equals(v, Vector128.Create((ushort)'/')), Vector128.Equals(v, Vector128.Create((ushort)'.'))).ExtractMostSignificantBits();
                _dot |= (compound >>> 8) << i;
                separator |= ((uint)(byte)compound) << i;
            }

            {
                var offset = length - Vector128<ushort>.Count;
                var v = Vector128.LoadUnsafe(ref source, (nuint)offset);
                var compound = Vector128.Narrow(Vector128.Equals(v, Vector128.Create((ushort)'/')), Vector128.Equals(v, Vector128.Create((ushort)'.'))).ExtractMostSignificantBits();
                dot = _dot | ((compound >>> 8) << offset);
                return separator | (((uint)(byte)compound) << offset);
            }
        }

        for (; i < length; ++i)
        {
            switch (Unsafe.Add(ref source, i))
            {
                case '/':
                    separator |= 1u << i;
                    break;
                case '.':
                    _dot |= 1u << i;
                    break;
            }
        }

        dot = _dot;
        return separator;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint CalculateSeparatorWall(uint separator, int length)
    {
        return (separator >>> 1) | ((uint.MaxValue >>> length) << length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ZeroHighBits(uint value, int index)
    {
        if (Bmi2.IsSupported)
        {
            return Bmi2.ZeroHighBits(value, (uint)(index & 31));
        }

        return value & (~(uint.MaxValue << index));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ResetLowestSetBit(uint value)
    {
        if (Bmi1.IsSupported)
        {
            return Bmi1.ResetLowestSetBit(value);
        }

        return value & (value - 1);
    }
}

```

# 性能総括

`uint`型と`ulong`型で思ったほど性能に差が出ませんでしたので、多分dotnet/runtimeに本採用されることがあれば、`ulong`型に対する`LeadingZeroCount`をサポートしていないプロセッサも世界には現役で存在していますから`uint`型のみで行くのではないでしょうか。

以下のBenchmarkDotNetの表はとりあえずUnix版のみに絞った場合の性能比較表です。
各`Method`名の意味は以下の通りです。

- `Old`: 旧実装
- `Full`: `Path.GetFullPath`
- `ReverseEach`: 純朴な逆順走査の参考実装
- `ReverseSimd32`: `uint`型で都度都度セグメントスタックを伸長させる実装
- `ReverseSimd64`: `ulong`型で都度都度セグメントスタックを伸長させる実装
- `ReverseEachNoTrim`: `ReverseEach`では`ReadOnlySpan<char>.Trim`で最初と最後の`/`をTrimしていましたが、別にしなくてもアルゴリズム上は問題ないので性能比較をしてみようという実装
- `ReverseSimd32/64NoTrim`: 同上
- `AllocOnce`: 都度都度セグメントスタックが伸長するとアロケーションが度々発生してしまうので必要そうなセグメントの長さを推定して事前に確保してしまう実装

**Meanが小さければ小さいほど実行時間が短いので性能は良い**です。

```
BenchmarkDotNet v0.15.2, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.101
  [Host]   : .NET 10.0.1 (10.0.125.57005), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.1 (10.0.125.57005), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  
```

| Method              | Source               | Mean        | Error      | StdDev    | Ratio | RatioSD |
|-------------------- |--------------------- |------------:|-----------:|----------:|------:|--------:|
| **ReverseEach**         | **../..(...)xerea [69]** |   **124.46 ns** |  **22.043 ns** |  **1.208 ns** |  **0.67** |    **0.01** |
| ReverseSimd32       | ../..(...)xerea [69] |   134.87 ns |   6.181 ns |  0.339 ns |  0.73 |    0.00 |
| ReverseSimd64       | ../..(...)xerea [69] |   136.66 ns |   9.376 ns |  0.514 ns |  0.74 |    0.00 |
| ReverseEachNoTrim   | ../..(...)xerea [69] |   122.96 ns |  15.949 ns |  0.874 ns |  0.66 |    0.00 |
| ReverseSimd32NoTrim | ../..(...)xerea [69] |   134.83 ns |   9.862 ns |  0.541 ns |  0.73 |    0.00 |
| ReverseSimd64NoTrim | ../..(...)xerea [69] |   136.85 ns |   2.537 ns |  0.139 ns |  0.74 |    0.00 |
| AllocOnce           | ../..(...)xerea [69] |   120.61 ns |  16.760 ns |  0.919 ns |  0.65 |    0.01 |
| Old                 | ../..(...)xerea [69] |   185.10 ns |  15.734 ns |  0.862 ns |  1.00 |    0.01 |
|                     |                      |             |            |           |       |         |
| **ReverseEach**         | **////(...)abcd [1022]** | **2,127.99 ns** | **703.230 ns** | **38.546 ns** |  **0.87** |    **0.01** |
| ReverseSimd32       | ////(...)abcd [1022] |   568.29 ns | 111.665 ns |  6.121 ns |  0.23 |    0.00 |
| ReverseSimd64       | ////(...)abcd [1022] |   504.34 ns | 178.216 ns |  9.769 ns |  0.21 |    0.00 |
| ReverseEachNoTrim   | ////(...)abcd [1022] | 2,033.67 ns |  72.259 ns |  3.961 ns |  0.83 |    0.00 |
| ReverseSimd32NoTrim | ////(...)abcd [1022] |   577.37 ns | 124.965 ns |  6.850 ns |  0.24 |    0.00 |
| ReverseSimd64NoTrim | ////(...)abcd [1022] |   576.16 ns |  93.784 ns |  5.141 ns |  0.24 |    0.00 |
| AllocOnce           | ////(...)abcd [1022] |   454.23 ns | 193.471 ns | 10.605 ns |  0.19 |    0.00 |
| Old                 | ////(...)abcd [1022] | 2,442.52 ns | 111.447 ns |  6.109 ns |  1.00 |    0.00 |
|                     |                      |             |            |           |       |         |
| **ReverseEach**         | **/102(...)abcd [1025]** | **1,187.65 ns** |  **87.640 ns** |  **4.804 ns** |  **0.52** |    **0.01** |
| ReverseSimd32       | /102(...)abcd [1025] |   292.93 ns |   6.127 ns |  0.336 ns |  0.13 |    0.00 |
| ReverseSimd64       | /102(...)abcd [1025] |   185.10 ns |  15.173 ns |  0.832 ns |  0.08 |    0.00 |
| ReverseEachNoTrim   | /102(...)abcd [1025] | 1,388.82 ns |  19.245 ns |  1.055 ns |  0.60 |    0.01 |
| ReverseSimd32NoTrim | /102(...)abcd [1025] |   297.22 ns |   3.809 ns |  0.209 ns |  0.13 |    0.00 |
| ReverseSimd64NoTrim | /102(...)abcd [1025] |   296.76 ns |   3.679 ns |  0.202 ns |  0.13 |    0.00 |
| AllocOnce           | /102(...)abcd [1025] |   130.54 ns |   3.051 ns |  0.167 ns |  0.06 |    0.00 |
| Old                 | /102(...)abcd [1025] | 2,304.08 ns | 569.383 ns | 31.210 ns |  1.00 |    0.02 |
|                     |                      |             |            |           |       |         |
| **ReverseEach**         | **/some(...)ments [45]** |    **53.66 ns** |   **3.913 ns** |  **0.215 ns** |  **0.48** |    **0.00** |
| ReverseSimd32       | /some(...)ments [45] |    36.01 ns |   0.387 ns |  0.021 ns |  0.32 |    0.00 |
| ReverseSimd64       | /some(...)ments [45] |    32.98 ns |   2.103 ns |  0.115 ns |  0.29 |    0.00 |
| ReverseEachNoTrim   | /some(...)ments [45] |    48.01 ns |   0.194 ns |  0.011 ns |  0.43 |    0.00 |
| ReverseSimd32NoTrim | /some(...)ments [45] |    35.17 ns |   0.222 ns |  0.012 ns |  0.31 |    0.00 |
| ReverseSimd64NoTrim | /some(...)ments [45] |    35.14 ns |   0.407 ns |  0.022 ns |  0.31 |    0.00 |
| AllocOnce           | /some(...)ments [45] |    35.48 ns |   2.498 ns |  0.137 ns |  0.32 |    0.00 |
| Old                 | /some(...)ments [45] |   112.19 ns |   0.478 ns |  0.026 ns |  1.00 |    0.00 |
|                     |                      |             |            |           |       |         |
| **ReverseEach**         | **/som(...)ers/ [216]**  |   **285.88 ns** |  **60.765 ns** |  **3.331 ns** |  **0.59** |    **0.01** |
| ReverseSimd32       | /som(...)ers/ [216]  |    79.73 ns |   0.910 ns |  0.050 ns |  0.17 |    0.00 |
| ReverseSimd64       | /som(...)ers/ [216]  |    62.83 ns |   0.493 ns |  0.027 ns |  0.13 |    0.00 |
| ReverseEachNoTrim   | /som(...)ers/ [216]  |   281.66 ns |  80.795 ns |  4.429 ns |  0.58 |    0.01 |
| ReverseSimd32NoTrim | /som(...)ers/ [216]  |    81.17 ns |   4.693 ns |  0.257 ns |  0.17 |    0.00 |
| ReverseSimd64NoTrim | /som(...)ers/ [216]  |    79.24 ns |   0.956 ns |  0.052 ns |  0.16 |    0.00 |
| AllocOnce           | /som(...)ers/ [216]  |    52.15 ns |   0.613 ns |  0.034 ns |  0.11 |    0.00 |
| Old                 | /som(...)ers/ [216]  |   483.05 ns |  19.828 ns |  1.087 ns |  1.00 |    0.00 |
|                     |                      |             |            |           |       |         |
| **ReverseEach**         | **/som(...)piyo [122]**  |   **181.94 ns** |  **16.577 ns** |  **0.909 ns** |  **0.64** |    **0.00** |
| ReverseSimd32       | /som(...)piyo [122]  |    51.77 ns |   0.628 ns |  0.034 ns |  0.18 |    0.00 |
| ReverseSimd64       | /som(...)piyo [122]  |    63.62 ns |   4.932 ns |  0.270 ns |  0.23 |    0.00 |
| ReverseEachNoTrim   | /som(...)piyo [122]  |   175.81 ns |   8.183 ns |  0.449 ns |  0.62 |    0.00 |
| ReverseSimd32NoTrim | /som(...)piyo [122]  |    51.67 ns |   1.137 ns |  0.062 ns |  0.18 |    0.00 |
| ReverseSimd64NoTrim | /som(...)piyo [122]  |    51.54 ns |   0.724 ns |  0.040 ns |  0.18 |    0.00 |
| AllocOnce           | /som(...)piyo [122]  |    56.47 ns |   0.480 ns |  0.026 ns |  0.20 |    0.00 |
| Old                 | /som(...)piyo [122]  |   282.50 ns |  34.609 ns |  1.897 ns |  1.00 |    0.01 |
|                     |                      |             |            |           |       |         |
| **ReverseEach**         | **abc/(...)/../ [165]**  |   **410.05 ns** |  **27.724 ns** |  **1.520 ns** |  **0.86** |    **0.00** |
| ReverseSimd32       | abc/(...)/../ [165]  |   151.39 ns |  16.826 ns |  0.922 ns |  0.32 |    0.00 |
| ReverseSimd64       | abc/(...)/../ [165]  |   142.56 ns |   3.269 ns |  0.179 ns |  0.30 |    0.00 |
| ReverseEachNoTrim   | abc/(...)/../ [165]  |   360.18 ns |   7.777 ns |  0.426 ns |  0.76 |    0.00 |
| ReverseSimd32NoTrim | abc/(...)/../ [165]  |   148.73 ns |   5.473 ns |  0.300 ns |  0.31 |    0.00 |
| ReverseSimd64NoTrim | abc/(...)/../ [165]  |   147.89 ns |   3.358 ns |  0.184 ns |  0.31 |    0.00 |
| AllocOnce           | abc/(...)/../ [165]  |   146.33 ns |   2.392 ns |  0.131 ns |  0.31 |    0.00 |
| Old                 | abc/(...)/../ [165]  |   474.46 ns |  11.765 ns |  0.645 ns |  1.00 |    0.00 |

```
BenchmarkDotNet v0.15.2, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.101
  [Host]   : .NET 10.0.1 (10.0.125.57005), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.1 (10.0.125.57005), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  
```

| Method              | Source               | Mean          | Error       | StdDev     | Ratio | RatioSD |
|-------------------- |--------------------- |--------------:|------------:|-----------:|------:|--------:|
| **ReverseEach**         | **/**                    |     **1.3280 ns** |   **0.4566 ns** |  **0.0250 ns** |  **0.10** |    **0.00** |
| ReverseSimd32       | /                    |     1.2143 ns |   0.0886 ns |  0.0049 ns |  0.09 |    0.00 |
| ReverseSimd64       | /                    |     1.2217 ns |   0.0907 ns |  0.0050 ns |  0.09 |    0.00 |
| ReverseEachNoTrim   | /                    |     1.3043 ns |   0.4686 ns |  0.0257 ns |  0.10 |    0.00 |
| ReverseSimd32NoTrim | /                    |     1.2977 ns |   0.3084 ns |  0.0169 ns |  0.10 |    0.00 |
| ReverseSimd64NoTrim | /                    |     1.2926 ns |   0.0929 ns |  0.0051 ns |  0.10 |    0.00 |
| AllocOnce           | /                    |     0.5854 ns |   0.0346 ns |  0.0019 ns |  0.04 |    0.00 |
| Old                 | /                    |    13.0315 ns |   1.3780 ns |  0.0755 ns |  1.00 |    0.01 |
| Full                | /                    |     8.9509 ns |   0.0120 ns |  0.0007 ns |  0.69 |    0.00 |
|                     |                      |               |             |            |       |         |
| **ReverseEach**         | **//**                   |     **2.3353 ns** |   **0.0417 ns** |  **0.0023 ns** |  **0.09** |    **0.00** |
| ReverseSimd32       | //                   |     2.3524 ns |   0.2115 ns |  0.0116 ns |  0.09 |    0.00 |
| ReverseSimd64       | //                   |     2.3435 ns |   0.1627 ns |  0.0089 ns |  0.09 |    0.00 |
| ReverseEachNoTrim   | //                   |     1.8097 ns |   0.4219 ns |  0.0231 ns |  0.07 |    0.00 |
| ReverseSimd32NoTrim | //                   |     1.7971 ns |   0.0249 ns |  0.0014 ns |  0.07 |    0.00 |
| ReverseSimd64NoTrim | //                   |     1.7915 ns |   0.0502 ns |  0.0027 ns |  0.07 |    0.00 |
| AllocOnce           | //                   |     1.0936 ns |   0.0569 ns |  0.0031 ns |  0.04 |    0.00 |
| Old                 | //                   |    25.2618 ns |   1.3875 ns |  0.0761 ns |  1.00 |    0.00 |
| Full                | //                   |    19.9078 ns |   1.8609 ns |  0.1020 ns |  0.79 |    0.00 |
|                     |                      |               |             |            |       |         |
| **ReverseEach**         | **/102(...)abcd [1025]** | **1,201.0465 ns** |  **61.3042 ns** |  **3.3603 ns** |  **0.53** |    **0.00** |
| ReverseSimd32       | /102(...)abcd [1025] |   294.9355 ns |  10.2815 ns |  0.5636 ns |  0.13 |    0.00 |
| ReverseSimd64       | /102(...)abcd [1025] |   293.6021 ns |   0.2684 ns |  0.0147 ns |  0.13 |    0.00 |
| ReverseEachNoTrim   | /102(...)abcd [1025] | 1,400.4987 ns |  13.2415 ns |  0.7258 ns |  0.62 |    0.00 |
| ReverseSimd32NoTrim | /102(...)abcd [1025] |   294.5738 ns |   0.8289 ns |  0.0454 ns |  0.13 |    0.00 |
| ReverseSimd64NoTrim | /102(...)abcd [1025] |   294.9437 ns |   1.2796 ns |  0.0701 ns |  0.13 |    0.00 |
| AllocOnce           | /102(...)abcd [1025] |   130.5360 ns |   4.2363 ns |  0.2322 ns |  0.06 |    0.00 |
| Old                 | /102(...)abcd [1025] | 2,262.8620 ns |  84.3282 ns |  4.6223 ns |  1.00 |    0.00 |
| Full                | /102(...)abcd [1025] | 1,618.8595 ns |  49.1121 ns |  2.6920 ns |  0.72 |    0.00 |
|                     |                      |               |             |            |       |         |
| **ReverseEach**         | **/some(...)ments [45]** |    **53.2671 ns** |   **1.5355 ns** |  **0.0842 ns** |  **0.49** |    **0.00** |
| ReverseSimd32       | /some(...)ments [45] |    32.9418 ns |   0.6578 ns |  0.0361 ns |  0.30 |    0.00 |
| ReverseSimd64       | /some(...)ments [45] |    34.2760 ns |   1.1249 ns |  0.0617 ns |  0.31 |    0.00 |
| ReverseEachNoTrim   | /some(...)ments [45] |    48.1194 ns |   1.5861 ns |  0.0869 ns |  0.44 |    0.00 |
| ReverseSimd32NoTrim | /some(...)ments [45] |    35.1787 ns |   0.3539 ns |  0.0194 ns |  0.32 |    0.00 |
| ReverseSimd64NoTrim | /some(...)ments [45] |    36.8087 ns |   0.7768 ns |  0.0426 ns |  0.34 |    0.00 |
| AllocOnce           | /some(...)ments [45] |    35.8168 ns |   3.3467 ns |  0.1834 ns |  0.33 |    0.00 |
| Old                 | /some(...)ments [45] |   109.4133 ns |   9.5064 ns |  0.5211 ns |  1.00 |    0.01 |
| Full                | /some(...)ments [45] |    76.0014 ns |   4.5768 ns |  0.2509 ns |  0.69 |    0.00 |
|                     |                      |               |             |            |       |         |
| **ReverseEach**         | **/som(...)ers/ [216]**  |   **343.6006 ns** | **208.0391 ns** | **11.4033 ns** |  **0.71** |    **0.02** |
| ReverseSimd32       | /som(...)ers/ [216]  |    80.0896 ns |   0.8941 ns |  0.0490 ns |  0.17 |    0.00 |
| ReverseSimd64       | /som(...)ers/ [216]  |    80.3955 ns |   0.9611 ns |  0.0527 ns |  0.17 |    0.00 |
| ReverseEachNoTrim   | /som(...)ers/ [216]  |   274.6824 ns |   4.2409 ns |  0.2325 ns |  0.57 |    0.00 |
| ReverseSimd32NoTrim | /som(...)ers/ [216]  |    81.6192 ns |   2.9277 ns |  0.1605 ns |  0.17 |    0.00 |
| ReverseSimd64NoTrim | /som(...)ers/ [216]  |    79.8982 ns |   0.8281 ns |  0.0454 ns |  0.17 |    0.00 |
| AllocOnce           | /som(...)ers/ [216]  |    52.0060 ns |   0.7862 ns |  0.0431 ns |  0.11 |    0.00 |
| Old                 | /som(...)ers/ [216]  |   483.0733 ns |  34.2293 ns |  1.8762 ns |  1.00 |    0.00 |
| Full                | /som(...)ers/ [216]  |   350.0465 ns |  15.0017 ns |  0.8223 ns |  0.72 |    0.00 |
|                     |                      |               |             |            |       |         |
| **ReverseEach**         | **/som(...)piyo [122]**  |   **175.6242 ns** |   **7.8199 ns** |  **0.4286 ns** |  **0.63** |    **0.00** |
| ReverseSimd32       | /som(...)piyo [122]  |    51.4379 ns |   0.3351 ns |  0.0184 ns |  0.19 |    0.00 |
| ReverseSimd64       | /som(...)piyo [122]  |    52.6520 ns |   2.7305 ns |  0.1497 ns |  0.19 |    0.00 |
| ReverseEachNoTrim   | /som(...)piyo [122]  |   183.0963 ns |  42.7395 ns |  2.3427 ns |  0.66 |    0.01 |
| ReverseSimd32NoTrim | /som(...)piyo [122]  |    51.6134 ns |   0.2927 ns |  0.0160 ns |  0.19 |    0.00 |
| ReverseSimd64NoTrim | /som(...)piyo [122]  |    53.8256 ns |   1.0982 ns |  0.0602 ns |  0.19 |    0.00 |
| AllocOnce           | /som(...)piyo [122]  |    55.7150 ns |   2.0257 ns |  0.1110 ns |  0.20 |    0.00 |
| Old                 | /som(...)piyo [122]  |   277.2216 ns |  26.1982 ns |  1.4360 ns |  1.00 |    0.01 |
| Full                | /som(...)piyo [122]  |   199.0104 ns |   3.4706 ns |  0.1902 ns |  0.72 |    0.00 |

長くなれば長くなるほど`AllocOnce`の性能が良くなってきましたね。
そして`ReverseSimd32`と`ReverseSimd64`も500文字未満程度だと`AllocOnce`に勝ったりしてなかなか優秀ですし、お互いにほぼ差がつかない感じがありますね。
長さに応じて最適なアルゴリズムを選ぶべきでしょうね。

いずれにせよ`Old`相手には少なくとも3倍は速かったりします。`Full`にも基本的に負けず最低2倍高速です。

## 感想

実装するほどに疑問が生じ、ベンチマークを取りたくなりますね。
BenchmarkDotNetのバージョン間ABテスト自動化ソリューションとかないのですかね？
あるいはgitのbranch間での性能比較ソリューションが欲しいです。
手書きで2系統用意すると混乱するのです。

# 余談

## 他言語での実装について

以下の記事にまとめました。

https://zenn.dev/pcysl5edgo/articles/80bbadaf787aa4

プログラミング言語の初期の歴史で文字列型は`null terminated char*`として扱われることが多かったため、順方向走査で長さ不明な文字列を柔軟に処理できるようなアルゴリズムが好まれていたのでしょう。
そしてまああんまり誰も最適化をこだわらなかったのでしょうね。

## シフト演算子の右項(第2引数)について

C#には`<<`, `>>`, `>>>`の3つのシフト演算子が存在していますが、いずれも第2引数に取る型が`int`型です。.NET7から`IBinaryInteger<TSelf>`インターフェースが登場しましたが、これ実装しているのが`IShiftOperators<TSelf,Int32,TSelf>`なのでやはり`int`型しか第2引数に受け取らないのですよね……　整数型ならばなんでも受け入れてくれと正直思いますけれどもね。

受け取る型はまあ今回本題ではないのでさておくとして、実はシフトする際に第2引数の下位ビットだけを使用するようにマスクかかっていました。

```csharp
// 疑似コードですが大体これで伝わるでしょう。
static T <<(T left, int right)
{
    return left << (right & (sizeof(left) * 8 - 1));
}
```

ですので、仮に第1引数が`int`型の場合は第2引数に32を指定すると0バイトシフトして第1引数そのままが戻り値になることになります。
私は32バイトシフトして結果が0になると思い込んでいました。読者諸氏も気を付けてください。

## VectorX.ExtractMostSignificantBitsの陥穽について

`VectorX<byte>`以外の要素型サイズが非1byteの型(たとえば`VectorX<ushort>`)の`ExtractMostSignificantBits`は基本的に非常に非効率な実装となっておりますので避けた方が良いです。
`AVX512.IsSupported`が`true`な環境なら効率的という話はありますが。
https://github.com/dotnet/runtime/pull/110662


AVX512プログラミングはまた独特なので今回は採用を見送りました。

## Bmi2.ParallelBitExtractとVectorX.Narrowについて

x86のBmi2が使用可能な環境では`Bmi2.ParallelBitExtract`でも同じことが実現可能ではありました。
しかし、`Bmi2.ParallelBitExtract`つまり`pext`命令は[AMDのZen2アーキテクチャとそれ以前で桁2つ分遅い](https://yaneuraou.yaneu.com/2020/08/02/yaneuraou-ryzen-threadripper-3990x-optimization/)という話もありましたので今回採用は見送らせてもらいました。2020年にZen3が登場したのでまだ5年と考えると一応無視するのも悪いですからね。
ベンチマーク取ろうにも手元にARMもZen2もないですしね……

## Bmi1.ResetLowestSetBit

`bit mask`の最下位bitだけを0クリアすることで`TrailingZeroCount`と組み合わせて高速にforeachを回したくなるでしょう。
0クリアするのは`x &= -x`または`x &= x - 1`というbit操作で実現可能です。x86では`Bmi1.ResetLowestSetBit`を使うことでより高速に計算できるとされています。
なお、参考文献では差は出力されたアセンブリのサイズ以外に性能差はありませんでした。

そして今回は後方からの逆順走査なのであまり活用できませんでした。
bitを逆に並べ替えることにより、逆の逆で順方向走査にするという発想も私の脳裏に過りました。しかし、ARMには`ArmBase.ReverseElementBits`つまり`rbit`という32bitを逆順に並べ替える命令がありますが、x86にはどうやら単一命令としては無いようです。.NET10で追加された`Gfni.GaloisFieldAffineTransform`つまり`gf2p8affineqb`なる命令と`bswap`を使えば実現できるようです。
今回はベンチマーク書く労力を考慮して不採用としました。

https://zenn.dev/herumi/articles/extension-field-of-f2-x64
https://learn.microsoft.com/en-us/dotnet/api/system.runtime.intrinsics.x86.gfni?view=net-10.0

## bool value ? 1 : 0について

`bool`型の一般的な話をまずします。

- `sizeof(bool)`は1であり`byte`型と同じ大きさです。
- `true`と書くと実質`1`として表現されます。
- `false`と書くと実質`0`として表現されます。

故に`bool value ? 1 : 0`と記述すると条件式でありながら`if文`としては扱われず条件分岐命令になることもありません。ただの整数型として計算されることになります。CPUパイプラインフレンドリーですね。

なお、`Unsafe.BitCast<byte, bool>(32)`は`true`として扱われます。

## メモリアライメントについて

SIMDを扱うならメモリアライメントを64byte境界に揃えて使用することが奨励されています。
ただ…… .NETのマネージドメモリのメモリアライメントはSIMDで必要な単位(32とか64byte)では保証されていませんし、よくガベージコレクションによって動くので`fixed`文で固定しなくてはいけません。
この`fixed`文は「ガベージコレクションに対して極めて悪い影響を及ぼす」と漠然とした表現で警告されていますので、アライメントを揃えることによるSIMDの性能向上と比較してどうなるのかさっぱりわからないのですよね。
ベンチマーク取ろうにも手元に網羅的プロセッサコレクションなんてものがないですしね……
故に今回は`LoadUnsafe`でメモリアライメントを完全に無視してSIMDしています。

## ReadOnlySpan<T> spanとref T item, int lengthの比較

C#11から`ref struct`がフィールドに`ref T`型を持つことが出来るようになりました。
生の`ref T`を保持することの`ReadOnlySpan<T>`に対する優れた点は`VectorX.LoadUnsafe`や`Unsafe.Add`系の`ref T`を引数に取る関数を使いやすいということです。
逆に劣る点としてはデバッグ時に値が確認しにくい点です。なぜか正しい値を示さなかったりするのですよね。
デバッグが面倒なのでproof of concept段階では性能の卓越性が示されている限り`ReadOnlySpan<T>`で十分でしょう。

ちなみにコミットハッシュ`70437b8c`とコミットハッシュ`7b228e9e`を比較するとSpanによって数十ナノ秒を無駄にしていることがわかります。

## ARM系CPUではVector128<byte>のExtractMostSignificantBitsが遅い

[参考文献中に挙げられている参考URLがリンク切れしているのですが](https://developer.arm.com/community/arm-community-blogs/b/infrastructure-solutions-blog/posts/porting-x86-vector-bitmask-optimizations-to-arm-neon)、ARM64だと`ExtractMostSignificantBits`相当の命令が存在せずえらい回りくどい命令列になって遅くなってしまいます。
.NET10には惜しくも間に合いませんでしたが.NET11からは下記ハック相当のコードになるのであんまり遅くはならないようです。

https://stackoverflow.com/questions/74722950/convert-vector-compare-mask-into-bit-mask-in-aarch64-simd-or-arm-neon

## C#でSIMDする際におすすめのサイト

[オフィスデイタイム氏のSIMD命令表](https://officedaytime.com/tips/simd.html)は必ず読むべきです。
存在しないSIMD命令は裏で長い命令列に変換されてパフォーマンスペナルティが大きいです。例えば`VectorX<ushort>.ExtractMostSignificantBits`に相当するSIMD命令が存在しないことがわかります。また、内部動作に関してもわかりやすく図解してくれていますので色々理解しやすくなっています。ありがたいことです。

## Windows版について

一文字ずつ逆順走査するものは実装できましたし、旧実装と比較すれば性能は良いです。しかし、最適化するのはなかなか難しいですね。
今回詳細に解説する気になるほどの完成度には未だ至っていませんので、本記事では論じないことにします。

ちなみにUnix版と比較して以下の課題が新規に生じており難易度は高くなっていますので、読者諸氏はコーディングエージェントに良い感じのコードを書かせてみるのも面白いのではないでしょうか？

- 各セグメント末尾の連続する`.`は無視する
- `.`のみからなるセグメントは無視しない
- セパレータが`/`と`\`の2種類
- パスの始まり方でモードが色々ある

# 参考文献

https://simdjson.org/publications/
https://wunkolo.github.io/post/2020/05/pclmulqdq-tricks/
https://tearth.dev/posts/performance-of-bit-manipulation-instructions-bmi/
https://github.com/dotnet/runtime/issues/76047
https://officedaytime.com/tips/simd.html
Donald E. Knuth. 1974. Structured Programming with go to Statements. ACM Comput. Surv. 6, 4 (Dec. 1974), 261–301. 
https://doi.org/10.1145/356635.356640

> Yet we should not pass up our opportunities in that critical 3%

[^1]: Posixでは実は冒頭2連続の`//`は融合せずそのまま扱うこととなっていますが、概ね全てのシステムがこの例外事項を無視しています。
https://pubs.opengroup.org/onlinepubs/9699919799/basedefs/V1_chap03.html#tag_03_271