```

BenchmarkDotNet v0.15.2, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.100
  [Host]    : .NET 10.0.0 (10.0.25.52411), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  MediumRun : .NET 10.0.0 (10.0.25.52411), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method      | Source               | Mean      | Error    | StdDev   | Median    | Ratio | RatioSD |
|------------ |--------------------- |----------:|---------:|---------:|----------:|------:|--------:|
| **ReverseEach** | **../..(...)xerea [69]** | **161.67 ns** | **0.161 ns** | **0.231 ns** | **161.70 ns** |  **0.89** |    **0.00** |
| ReverseSimd | ../..(...)xerea [69] | 143.02 ns | 0.396 ns | 0.580 ns | 142.83 ns |  0.79 |    0.00 |
| Old         | ../..(...)xerea [69] | 181.94 ns | 0.188 ns | 0.270 ns | 181.90 ns |  1.00 |    0.00 |
|             |                      |           |          |          |           |       |         |
| **ReverseEach** | **/some(...)ments [45]** |  **61.10 ns** | **1.166 ns** | **1.746 ns** |  **60.83 ns** |  **0.52** |    **0.02** |
| ReverseSimd | /some(...)ments [45] |  18.70 ns | 0.018 ns | 0.025 ns |  18.71 ns |  0.16 |    0.00 |
| Old         | /some(...)ments [45] | 118.45 ns | 0.706 ns | 0.967 ns | 118.33 ns |  1.00 |    0.01 |
|             |                      |           |          |          |           |       |         |
| **ReverseEach** | **/som(...)ers/ [216]**  | **276.15 ns** | **1.202 ns** | **1.684 ns** | **275.08 ns** |  **0.50** |    **0.00** |
| ReverseSimd | /som(...)ers/ [216]  |  54.41 ns | 2.365 ns | 3.391 ns |  51.36 ns |  0.10 |    0.01 |
| Old         | /som(...)ers/ [216]  | 556.61 ns | 1.186 ns | 1.739 ns | 556.35 ns |  1.00 |    0.00 |
|             |                      |           |          |          |           |       |         |
| **ReverseEach** | **/som(...)piyo [122]**  | **196.74 ns** | **1.999 ns** | **2.867 ns** | **194.35 ns** |  **0.64** |    **0.01** |
| ReverseSimd | /som(...)piyo [122]  |  39.04 ns | 0.291 ns | 0.418 ns |  39.34 ns |  0.13 |    0.00 |
| Old         | /som(...)piyo [122]  | 308.23 ns | 0.618 ns | 0.887 ns | 308.15 ns |  1.00 |    0.00 |
