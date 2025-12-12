# Pcysl5edgo.RemoveRedundantPath

This is a optimization test project for [Add Path.RemoveRelativeSegments Api](https://github.com/dotnet/runtime/issues/2162)

# Benchmarks

- ReverseSimd <- My implementation with SIMD
- ReverseEach <- My implementation without SIMD
- Old <- [The implementation of the original pull request](https://github.com/dotnet/runtime/pull/37939)
- Full <- `string Path.GetFullPath(string)`

## Unix full path

[Link](./Unix.FullPathBenchmarks-report-github.md)

| Method      | Source               | Median     | Ratio |
|------------ |--------------------- |-----------:| -----:|
| ReverseSimd | /                    |   1.788 ns |  0.26 |
| Old         | /                    |  14.270 ns |  2.05 |
| Full        | /                    |   6.954 ns |  1.00 |
|             |                      |            |       |
| ReverseSimd | /some(...)ments [45] |  18.708 ns |  0.21 |
| Old         | /some(...)ments [45] | 116.114 ns |  1.30 |
| Full        | /some(...)ments [45] |  89.530 ns |  1.00 |
|             |                      |            |       |
| ReverseSimd | /som(...)ers/ [216]  |  52.856 ns |  0.11 |
| Old         | /som(...)ers/ [216]  | 551.466 ns |  1.16 |
| Full        | /som(...)ers/ [216]  | 475.696 ns |  1.00 |

## Unix path including relative segment(s)

[Link](./Unix.RelativePathBenchmarks-report-github.md)

| Method      | Source               | Median    | Ratio |
|------------ |--------------------- |----------:| -----:|
| ReverseSimd | ../..(...)xerea [69] | 142.83 ns |  0.79 |
| Old         | ../..(...)xerea [69] | 181.90 ns |  1.00 |

## Windows full path

[Link](./Windows.FullPathBenchmarks-report-github.md)

| Method      | Source                  | Median     | Ratio |
|------------ |------------------------ |-----------:| -----:|
| ReverseEach | \\?\U(...)\0.py [78]    |   6.228 ns |  1.34 |
| Old         | \\?\U(...)\0.py [78]    |  15.564 ns |  3.35 |
| Full        | \\?\U(...)\0.py [78]    |   4.650 ns |  1.00 |
|             |                         |            |       |
| ReverseEach | \\Z:\ton\two\           |  42.938 ns |  0.54 |
| Old         | \\Z:\ton\two\           |  70.423 ns |  0.89 |
| Full        | \\Z:\ton\two\           |  79.561 ns |  1.00 |
|             |                         |            |       |
| ReverseEach | A:\Us(...).exe [30]     |  48.608 ns |  0.47 |
| Old         | A:\Us(...).exe [30]     | 107.807 ns |  1.04 |  
| Full        | A:\Us(...).exe [30]     | 103.597 ns |  1.00 |
|             |                         |            |       |
| ReverseEach | C:\Pr(...)e.bat [56]    |  64.656 ns |  0.43 |
| Old         | C:\Pr(...)e.bat [56]    | 142.135 ns |  0.94 |
| Full        | C:\Pr(...)e.bat [56]    | 150.918 ns |  1.00 |

## Windows path including relative segment(s)

[Link](./Windows.RelativePathBenchmarks-report-github.md)

| Method      | Source               | Median     | Ratio |
|------------ |--------------------- |-----------:| -----:|
| ReverseEach | ../..(...)xerea [69] | 170.270 ns |  0.56 |
| Old         | ../..(...)xerea [69] | 301.621 ns |  1.00 |
|             |                      |            |       |
| ReverseEach | //.\D(...)/.... [29] |  88.199 ns |  0.63 |
| Old         | //.\D(...)/.... [29] | 139.019 ns |  1.00 |
|             |                      |            |       |
| ReverseEach | /som(...)ers/ [216]  | 465.841 ns |  0.71 |
| Old         | /som(...)ers/ [216]  | 655.064 ns |  1.00 |
|             |                      |            |       |
| ReverseEach | \??\..\abcdef/.../// |   5.624 ns |  0.54 |
| Old         | \??\..\abcdef/.../// |  10.320 ns |  1.00 |
|             |                      |            |       |
| ReverseEach | \\?..\abcdef/...///  |  75.329 ns |  0.63 |
| Old         | \\?..\abcdef/...///  | 118.884 ns |  1.00 |