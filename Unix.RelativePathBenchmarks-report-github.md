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
