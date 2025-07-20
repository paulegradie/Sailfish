---
title: Enterprise Licensing Integration
---

Learn how to integrate Sailfish enterprise licensing into your performance testing workflow and unlock advanced features.

{% info-callout title="Enterprise Features" %}
Enterprise licensing enables advanced features like ScaleFish complexity analysis, enhanced SailDiff capabilities, priority support, and enterprise-grade analytics.
{% /info-callout %}

## 🔑 License Key Setup

### Environment Variable (Recommended)

{% success-callout title="Secure Configuration" %}
Set your license key as an environment variable for secure, centralized configuration across your development and production environments.
{% /success-callout %}

```bash
# Set environment variable
export SAILFISH_LICENSE_KEY="ABCD1234-EFGH5678-IJKL9012-MNOP3456"

# Or in your .env file
SAILFISH_LICENSE_KEY=ABCD1234-EFGH5678-IJKL9012-MNOP3456
```

### Programmatic Configuration

```csharp
using Sailfish.Enterprise;

// Set license key programmatically
SailfishLicense.SetLicenseKey("ABCD1234-EFGH5678-IJKL9012-MNOP3456");

// Or configure during startup
var settings = RunSettingsBuilder
    .CreateBuilder()
    .WithEnterpriseLicense("ABCD1234-EFGH5678-IJKL9012-MNOP3456")
    .Build();
```

## 🚀 Feature Gating

### Automatic Feature Detection

{% code-callout title="Seamless Integration" %}
Sailfish automatically detects your enterprise license and enables advanced features without additional configuration.
{% /code-callout %}

```csharp
[Sailfish]
public class EnterprisePerformanceTest
{
    [SailfishVariable(scalefish: true, 10, 100, 1000)]
    public int DataSize { get; set; }

    [SailfishMethod]
    public async Task ComplexityAnalysisTest()
    {
        // ScaleFish automatically enabled with enterprise license
        await ProcessData(DataSize);
    }
}
```

### Manual Feature Checks

```csharp
using Sailfish.Enterprise;

public class AdvancedTestSuite
{
    public async Task RunAdvancedTests()
    {
        var license = await SailfishLicense.ValidateAsync();
        
        if (license.HasFeature("scalefish"))
        {
            await RunComplexityAnalysis();
        }
        
        if (license.HasFeature("saildiff"))
        {
            await RunRegressionAnalysis();
        }
        
        if (license.HasFeature("enterpriseFeatures"))
        {
            await RunEnterpriseOnlyTests();
        }
    }
}
```

## 📊 Usage Tracking

### Automatic Tracking

{% tip-callout title="Built-in Analytics" %}
Enterprise licenses automatically track feature usage for analytics and compliance monitoring.
{% /tip-callout %}

```csharp
// Usage is automatically tracked when features are used
[SailfishMethod]
public async Task TrackedPerformanceTest()
{
    // This usage will be automatically tracked
    await SomePerformanceOperation();
}
```

### Custom Usage Tracking

```csharp
using Sailfish.Enterprise;

public class CustomAnalytics
{
    public async Task TrackCustomUsage()
    {
        await UsageTracker.TrackAsync("custom_feature", new
        {
            testName = "CustomPerformanceTest",
            duration = TimeSpan.FromSeconds(5.2),
            dataPoints = 1000
        });
    }
}
```

## 🔧 Enterprise Configuration

### Advanced Settings

{% feature-grid columns=2 %}
{% feature-card title="Analytics Dashboard" description="Real-time performance monitoring and historical trend analysis." /%}

{% feature-card title="Team Management" description="User roles, permissions, and collaboration tools for enterprise teams." /%}

{% feature-card title="Custom Integrations" description="API access and webhook support for CI/CD pipeline integration." /%}

{% feature-card title="Compliance Features" description="SOC 2, GDPR compliance features and audit trail capabilities." /%}
{% /feature-grid %}

```json
{
  "SailfishSettings": {
    "DisableOverheadEstimation": false,
    "SampleSizeOverride": 50
  },
  "EnterpriseSettings": {
    "EnableAnalyticsDashboard": true,
    "EnableUsageTracking": true,
    "EnableTeamManagement": true,
    "ComplianceMode": "SOC2",
    "CustomIntegrations": {
      "WebhookUrl": "https://your-api.com/sailfish-webhook",
      "ApiKey": "your-api-key"
    }
  }
}
```

## 🛡️ License Validation

### Validation Process

{% warning-callout title="License Validation" %}
Sailfish validates your enterprise license at startup and periodically during execution to ensure compliance and feature availability.
{% /warning-callout %}

```csharp
public class LicenseValidationExample
{
    public async Task ValidateLicense()
    {
        var validation = await SailfishLicense.ValidateAsync();
        
        if (!validation.IsValid)
        {
            Console.WriteLine($"License validation failed: {validation.Error}");
            Console.WriteLine($"Error code: {validation.ErrorCode}");
            return;
        }
        
        Console.WriteLine($"License valid until: {validation.ExpiresAt}");
        Console.WriteLine($"Company: {validation.CompanyName}");
        Console.WriteLine($"Features: {string.Join(", ", validation.EnabledFeatures)}");
    }
}
```

### Offline Validation

```csharp
// For air-gapped environments
var offlineLicense = SailfishLicense.LoadFromFile("sailfish-license.json");
var isValid = offlineLicense.ValidateOffline();
```

## 🔄 CI/CD Integration

### GitHub Actions

{% code-callout title="Automated Testing" %}
Integrate enterprise Sailfish testing into your CI/CD pipeline with secure license key management.
{% /code-callout %}

```yaml
name: Performance Tests
on: [push, pull_request]

jobs:
  performance-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
          
      - name: Run Sailfish Performance Tests
        env:
          SAILFISH_LICENSE_KEY: ${{ secrets.SAILFISH_LICENSE_KEY }}
        run: |
          dotnet test --configuration Release
          
      - name: Upload Performance Reports
        uses: actions/upload-artifact@v3
        with:
          name: performance-reports
          path: SailfishIDETestOutput/
```

### Azure DevOps

```yaml
trigger:
- main

pool:
  vmImage: 'ubuntu-latest'

variables:
  SAILFISH_LICENSE_KEY: $(SailfishLicenseKey)

steps:
- task: DotNetCoreCLI@2
  displayName: 'Run Performance Tests'
  inputs:
    command: 'test'
    projects: '**/*PerformanceTests.csproj'
    arguments: '--configuration Release'
```

## 📈 Enterprise Analytics

### Dashboard Access

{% success-callout title="Real-time Insights" %}
Access your enterprise dashboard to view usage analytics, team performance, and compliance reports.
{% /success-callout %}

```csharp
// Access analytics programmatically
var analytics = await SailfishAnalytics.GetEnterpriseReportAsync(
    licenseKey: "your-license-key",
    period: TimeSpan.FromDays(30)
);

Console.WriteLine($"Total tests run: {analytics.TotalTests}");
Console.WriteLine($"Average execution time: {analytics.AverageExecutionTime}");
Console.WriteLine($"Performance improvement: {analytics.PerformanceImprovement}%");
```

### Custom Metrics

```csharp
// Define custom metrics for your organization
await SailfishAnalytics.TrackCustomMetricAsync("deployment_performance", new
{
    deploymentId = "deploy-123",
    environment = "production",
    performanceScore = 95.2,
    regressionDetected = false
});
```

## 🆘 Support & Troubleshooting

### Priority Support

{% tip-callout title="Enterprise Support" %}
Enterprise customers receive priority support with guaranteed 4-hour response times and direct access to our engineering team.
{% /tip-callout %}

**Contact Methods:**
- **Email**: enterprise-support@sailfish.dev
- **Phone**: +1 (555) 123-SAIL
- **Slack**: Join our enterprise Slack channel
- **Video Calls**: Schedule directly with your account manager

### Common Issues

**License Validation Failures:**
```csharp
// Check license status
var status = await SailfishLicense.GetStatusAsync();
Console.WriteLine($"License status: {status.Status}");
Console.WriteLine($"Days until expiry: {status.DaysUntilExpiry}");
```

**Feature Not Available:**
```csharp
// Verify feature availability
var hasFeature = await SailfishLicense.HasFeatureAsync("scalefish");
if (!hasFeature)
{
    Console.WriteLine("ScaleFish requires an enterprise license");
    Console.WriteLine("Visit https://sailfish.dev/pricing to upgrade");
}
```

{% note-callout title="Need Help?" %}
If you encounter any issues with enterprise licensing, our support team is here to help. Contact enterprise-support@sailfish.dev or visit your [Enterprise Dashboard](/enterprise/dashboard) for assistance.
{% /note-callout %}
