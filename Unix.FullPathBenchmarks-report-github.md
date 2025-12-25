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
