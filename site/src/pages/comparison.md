# Sailfish vs. Alternatives

**How does Sailfish compare to other performance testing tools?**

We believe in transparency. Here's an honest comparison of Sailfish with other popular performance testing tools to help you make the best choice for your team.

---

## Sailfish vs. BenchmarkDotNet

### **BenchmarkDotNet**
**Best for:** Simple microbenchmarks and method-level performance testing

**Strengths:**
- Excellent for microbenchmarking individual methods
- Strong statistical analysis for small-scale tests
- Good integration with .NET ecosystem
- Free and open source

**Limitations:**
- Limited to method-level testing
- No built-in CI/CD integration
- Minimal reporting and visualization
- No team collaboration features
- No enterprise support

### **Sailfish**
**Best for:** Comprehensive application performance testing with team collaboration

**Advantages over BenchmarkDotNet:**
- **Application-level testing** - Test entire workflows, not just methods
- **Advanced statistical analysis** - Machine learning-powered complexity estimation
- **Enterprise features** - Team management, advanced reporting, priority support
- **CI/CD integration** - Seamless integration with your development pipeline
- **Regression detection** - Automated before/after comparison with SailDiff
- **Scalability analysis** - Predict performance at different scales with ScaleFish

**When to choose Sailfish:** When you need more than microbenchmarks - testing complete user scenarios, API endpoints, or complex workflows with team collaboration and enterprise support.

---

## Sailfish vs. NBomber

### **NBomber**
**Best for:** Load testing and stress testing applications

**Strengths:**
- Excellent for load and stress testing
- Good for testing under high concurrency
- F#-based with functional programming approach
- Open source

**Limitations:**
- Focused primarily on load testing
- Limited statistical analysis capabilities
- No built-in regression detection
- Minimal enterprise features
- Steeper learning curve for non-F# developers

### **Sailfish**
**Best for:** Performance testing with statistical analysis and team collaboration

**Advantages over NBomber:**
- **Broader testing scope** - Performance, regression, and complexity analysis
- **Advanced analytics** - Statistical analysis and machine learning insights
- **Easier adoption** - C#-based with familiar syntax and patterns
- **Enterprise ready** - Team management, compliance, and priority support
- **Automated insights** - Intelligent analysis and recommendations
- **Development integration** - Works like a test framework in your IDE

**When to choose Sailfish:** When you need comprehensive performance insights beyond load testing, with statistical analysis and enterprise-grade features.

---

## Sailfish vs. k6

### **k6**
**Best for:** JavaScript-based load testing and API testing

**Strengths:**
- JavaScript-based testing scripts
- Good for API and web application load testing
- Cloud and on-premise options
- Strong community

**Limitations:**
- Primarily focused on load testing
- Limited .NET ecosystem integration
- No built-in statistical analysis
- Requires separate tools for regression detection
- JavaScript knowledge required

### **Sailfish**
**Best for:** .NET-focused performance testing with advanced analytics

**Advantages over k6:**
- **Native .NET integration** - Works seamlessly with C# applications
- **Statistical analysis** - Built-in complexity estimation and trend analysis
- **Regression detection** - Automated before/after comparison
- **IDE integration** - Run tests directly from Visual Studio or VS Code
- **Enterprise features** - Team collaboration and advanced reporting
- **No additional languages** - Use your existing C# skills

**When to choose Sailfish:** When working primarily with .NET applications and need more than load testing - statistical analysis, regression detection, and enterprise collaboration features.

---

## Feature Comparison Matrix

| Feature | Sailfish | BenchmarkDotNet | NBomber | k6 |
|---------|----------|-----------------|---------|-----|
| **Microbenchmarking** | ✅ | ✅ | ❌ | ❌ |
| **Application Testing** | ✅ | ❌ | ✅ | ✅ |
| **Load Testing** | ✅ | ❌ | ✅ | ✅ |
| **Statistical Analysis** | ✅ | ✅ | ❌ | ❌ |
| **Regression Detection** | ✅ | ❌ | ❌ | ❌ |
| **Complexity Estimation** | ✅ | ❌ | ❌ | ❌ |
| **CI/CD Integration** | ✅ | ⚠️ | ⚠️ | ✅ |
| **IDE Integration** | ✅ | ✅ | ❌ | ❌ |
| **Team Collaboration** | ✅ | ❌ | ❌ | ⚠️ |
| **Enterprise Support** | ✅ | ❌ | ❌ | ✅ |
| **Custom Reporting** | ✅ | ❌ | ❌ | ✅ |
| **On-Premise Deployment** | ✅ | N/A | N/A | ✅ |

**Legend:** ✅ Full Support | ⚠️ Limited Support | ❌ Not Available

---

## Why Teams Choose Sailfish

### **🎯 Comprehensive Solution**
Unlike tools that focus on just one aspect of performance testing, Sailfish provides end-to-end performance insights from microbenchmarks to application-level testing.

### **📊 Intelligent Analysis**
Built-in statistical analysis and machine learning capabilities provide insights that go beyond simple timing measurements.

### **🚀 Developer Experience**
Works like a familiar test framework - write tests in C#, run from your IDE, integrate with your existing workflow.

### **🏢 Enterprise Ready**
Team collaboration, advanced reporting, compliance features, and priority support for organizations that need more than open source tools.

### **🔄 Continuous Performance**
Automated regression detection and CI/CD integration help you catch performance issues before they reach production.

---

## Migration Guides

### **From BenchmarkDotNet**
Moving from BenchmarkDotNet to Sailfish is straightforward:

```csharp
// BenchmarkDotNet
[Benchmark]
public void MyBenchmark() { /* test code */ }

// Sailfish
[SailfishMethod]
public void MyTest() { /* test code */ }
```

**Benefits of migrating:**
- Keep your existing test logic
- Gain advanced analytics and reporting
- Add team collaboration features
- Get enterprise support and compliance

### **From NBomber**
Sailfish can complement or replace NBomber for .NET applications:

```csharp
// NBomber (F#)
let scenario = Scenario.Create("test", fun context -> task {
    // test logic
})

// Sailfish (C#)
[SailfishMethod]
public async Task Test() {
    // test logic
}
```

**Benefits of migrating:**
- Use familiar C# syntax
- Gain statistical analysis capabilities
- Add regression detection
- Integrate with your .NET development workflow

---

## Ready to Make the Switch?

**Try Sailfish risk-free and see the difference for yourself.**

<div style="text-align: center; margin: 3rem 0;">
  <div style="margin-bottom: 2rem;">
    <h3>Start Your Free Trial</h3>
    <p style="color: #64748b; margin-bottom: 2rem;">No credit card required • 5-minute setup • Full feature access</p>
  </div>
  
  <div style="display: flex; gap: 1rem; justify-content: center; flex-wrap: wrap;">
    <a href="/docs/0/getting-started" style="background: #3b82f6; color: white; padding: 16px 32px; border-radius: 8px; text-decoration: none; font-weight: 600; font-size: 18px;">Get Started Free →</a>
    <a href="/enterprise/contact" style="background: #059669; color: white; padding: 16px 32px; border-radius: 8px; text-decoration: none; font-weight: 600; font-size: 18px;">Talk to Sales →</a>
  </div>
  
  <p style="margin-top: 1rem; color: #64748b; font-size: 14px;">
    Questions about migration? Our team is here to help.
  </p>
</div>
