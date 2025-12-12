```

BenchmarkDotNet v0.15.2, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.100
  [Host]    : .NET 10.0.0 (10.0.25.52411), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  MediumRun : .NET 10.0.0 (10.0.25.52411), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method      | Source               | Mean       | Error     | StdDev    | Median     | Ratio | RatioSD |
|------------ |--------------------- |-----------:|----------:|----------:|-----------:|------:|--------:|
| **ReverseEach** | **/**                    |   **1.767 ns** | **0.0238 ns** | **0.0326 ns** |   **1.764 ns** |  **0.12** |    **0.00** |
| ReverseSimd | /                    |   1.788 ns | 0.0159 ns | 0.0232 ns |   1.788 ns |  0.13 |    0.00 |
| Old         | /                    |  14.265 ns | 0.2204 ns | 0.3299 ns |  14.270 ns |  1.00 |    0.03 |
| Full        | /                    |   6.891 ns | 0.0998 ns | 0.1463 ns |   6.954 ns |  0.48 |    0.01 |
|             |                      |            |           |           |            |       |         |
| **ReverseEach** | **//**                   |   **2.032 ns** | **0.0123 ns** | **0.0177 ns** |   **2.034 ns** |  **0.09** |    **0.00** |
| ReverseSimd | //                   |   2.009 ns | 0.0086 ns | 0.0127 ns |   2.012 ns |  0.09 |    0.00 |
| Old         | //                   |  23.482 ns | 0.0340 ns | 0.0488 ns |  23.477 ns |  1.00 |    0.00 |
| Full        | //                   |  18.849 ns | 0.0490 ns | 0.0719 ns |  18.868 ns |  0.80 |    0.00 |
|             |                      |            |           |           |            |       |         |
| **ReverseEach** | **/some(...)ments [45]** |  **61.768 ns** | **0.9499 ns** | **1.4217 ns** |  **61.219 ns** |  **0.53** |    **0.01** |
| ReverseSimd | /some(...)ments [45] |  18.705 ns | 0.0124 ns | 0.0165 ns |  18.708 ns |  0.16 |    0.00 |
| Old         | /some(...)ments [45] | 116.023 ns | 0.2569 ns | 0.3766 ns | 116.114 ns |  1.00 |    0.00 |
| Full        | /some(...)ments [45] |  89.560 ns | 2.3204 ns | 3.3278 ns |  89.530 ns |  0.77 |    0.03 |
|             |                      |            |           |           |            |       |         |
| **ReverseEach** | **/som(...)ers/ [216]**  | **278.250 ns** | **1.1204 ns** | **1.5336 ns** | **277.959 ns** |  **0.50** |    **0.00** |
| ReverseSimd | /som(...)ers/ [216]  |  52.264 ns | 0.4779 ns | 0.6854 ns |  52.856 ns |  0.09 |    0.00 |
| Old         | /som(...)ers/ [216]  | 551.489 ns | 1.5630 ns | 2.2910 ns | 551.466 ns |  1.00 |    0.01 |
| Full        | /som(...)ers/ [216]  | 475.520 ns | 2.2422 ns | 3.0692 ns | 475.696 ns |  0.86 |    0.01 |
|             |                      |            |           |           |            |       |         |
| **ReverseEach** | **/som(...)piyo [122]**  | **201.443 ns** | **0.7633 ns** | **1.1188 ns** | **201.107 ns** |  **0.66** |    **0.00** |
| ReverseSimd | /som(...)piyo [122]  |  38.691 ns | 0.0777 ns | 0.1162 ns |  38.654 ns |  0.13 |    0.00 |
| Old         | /som(...)piyo [122]  | 306.678 ns | 0.8624 ns | 1.2368 ns | 306.974 ns |  1.00 |    0.01 |
| Full        | /som(...)piyo [122]  | 263.236 ns | 0.4199 ns | 0.6285 ns | 263.075 ns |  0.86 |    0.00 |
