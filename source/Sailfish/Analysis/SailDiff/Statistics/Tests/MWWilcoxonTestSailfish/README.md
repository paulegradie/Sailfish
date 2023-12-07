# MannWhitneyWilcoxonTest

**Mann-Whitney-Wilcoxon test for unpaired samples**

### Aliases

Mann–Whitney U test also called the

- Mann–Whitney–Wilcoxon (MWW)
- Wilcoxon rank-sum test
- Wilcoxon–Mann–Whitney test)

## Usage

Note - this test is NOT the same as the Two Sample Wilcoxon Signed Rank Test

## Example

```csharp

double[] sample1 = { 4.6, 4.7, 4.9, 5.1, 5.2, 5.5, 5.8, 6.1, 6.5, 6.5, 7.2 };


double[] sample2 = { 5.2, 5.3, 5.4, 5.6, 6.2, 6.3, 6.8, 7.7, 8.0, 8.1 };


MannWhitneyWilcoxonTest test = new MannWhitneyWilcoxonTest(sample1, sample2,
  TwoSampleHypothesis.FirstValueIsSmallerThanSecond);

double sum1 = test.RankSum1; 
double sum2 = test.RankSum2; 

double statistic1 = test.Statistic1; 
double statistic2 = test.Statistic2; 

double pvalue = test.PValue; 


bool significant = test.Significant; 
```

It has greater efficiency than the t-test on non-normal distributions, such as a mixture of normal distributions, and it
is nearly as efficient as the t-test on normal distributions.

# Link

http:
