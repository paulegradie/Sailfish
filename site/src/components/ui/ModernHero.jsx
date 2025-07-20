import Link from 'next/link'
import { ArrowRightIcon, PlayIcon } from '@heroicons/react/24/outline'
import { Logo } from './Logo'
import { Button } from './Button'

// Stats component for social proof
function StatsSection() {
  const stats = [
    { label: 'Performance Tests Run', value: '1M+' },
    { label: 'Developers Using Sailfish', value: '10K+' },
    { label: 'Average Performance Improvement', value: '45%' },
    { label: 'Enterprise Customers', value: '100+' }
  ]

  return (
    <div className="mt-16 grid grid-cols-2 gap-8 md:grid-cols-4">
      {stats.map((stat) => (
        <div key={stat.label} className="text-center">
          <div className="text-2xl font-bold text-white md:text-3xl">
            {stat.value}
          </div>
          <div className="mt-1 text-sm text-slate-300">
            {stat.label}
          </div>
        </div>
      ))}
    </div>
  )
}

// Code example component
function CodeExample() {
  return (
    <div className="relative">
      <div className="absolute inset-0 rounded-2xl bg-gradient-to-tr from-primary-500 via-primary-600 to-secondary-500 opacity-10 blur-xl" />
      <div className="relative overflow-hidden rounded-2xl bg-slate-900/90 backdrop-blur-sm ring-1 ring-white/10">
        <div className="flex items-center gap-2 border-b border-slate-700 px-4 py-3">
          <div className="flex gap-1.5">
            <div className="h-3 w-3 rounded-full bg-red-500" />
            <div className="h-3 w-3 rounded-full bg-yellow-500" />
            <div className="h-3 w-3 rounded-full bg-green-500" />
          </div>
          <div className="text-sm text-slate-400">PerformanceTest.cs</div>
        </div>
        <div className="p-6">
          <pre className="text-sm text-slate-300">
            <code>{`[Sailfish]
public class ApiPerformanceTest
{
    [SailfishVariable(10, 100, 1000)]
    public int RequestCount { get; set; }

    [SailfishMethod]
    public async Task TestApiThroughput()
    {
        var client = new HttpClient();
        var tasks = Enumerable.Range(0, RequestCount)
            .Select(_ => client.GetAsync("/api/data"));
        
        await Task.WhenAll(tasks);
    }
}`}</code>
          </pre>
        </div>
      </div>
    </div>
  )
}

// Main hero component
export function ModernHero() {
  return (
    <div className="relative overflow-hidden bg-gradient-to-br from-slate-900 via-slate-800 to-slate-900">
      {/* Background decoration */}
      <div className="absolute inset-0">
        <div className="absolute top-0 left-1/2 -translate-x-1/2 -translate-y-1/2 w-[800px] h-[800px] bg-gradient-to-r from-primary-500/20 to-secondary-500/20 rounded-full blur-3xl" />
        <div className="absolute bottom-0 right-0 translate-x-1/2 translate-y-1/2 w-[600px] h-[600px] bg-gradient-to-l from-primary-600/10 to-secondary-600/10 rounded-full blur-3xl" />
      </div>

      <div className="relative mx-auto max-w-7xl px-4 py-24 sm:px-6 lg:px-8 lg:py-32">
        <div className="grid grid-cols-1 gap-16 lg:grid-cols-2 lg:gap-24">
          {/* Left column - Content */}
          <div className="flex flex-col justify-center">
            <div className="mb-8">
              <Logo size="lg" variant="light" />
            </div>
            
            <h1 className="text-4xl font-bold tracking-tight text-white sm:text-5xl lg:text-6xl">
              Performance Testing
              <span className="block bg-gradient-to-r from-primary-400 to-secondary-400 bg-clip-text text-transparent">
                Made Simple
              </span>
            </h1>
            
            <p className="mt-6 text-xl text-slate-300 leading-relaxed">
              Write performance tests that are <strong>simple</strong>, <strong>consistent</strong>, and <strong>familiar</strong>. 
              Sailfish brings statistical analysis and machine learning to .NET performance testing.
            </p>

            <div className="mt-8 flex flex-col gap-4 sm:flex-row sm:gap-6">
              <Link
                href="/docs/0/getting-started"
                className="inline-flex items-center justify-center gap-2 rounded-lg bg-primary-600 px-8 py-4 text-lg font-semibold text-white transition-all hover:bg-primary-700 hover:scale-105 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 focus:ring-offset-slate-900"
              >
                Get Started Free
                <ArrowRightIcon className="h-5 w-5" />
              </Link>
              
              <Link
                href="/enterprise/contact"
                className="inline-flex items-center justify-center gap-2 rounded-lg border-2 border-slate-600 px-8 py-4 text-lg font-semibold text-white transition-all hover:border-slate-500 hover:bg-slate-800 focus:outline-none focus:ring-2 focus:ring-slate-500 focus:ring-offset-2 focus:ring-offset-slate-900"
              >
                Enterprise Demo
                <PlayIcon className="h-5 w-5" />
              </Link>
            </div>

            <div className="mt-8 flex items-center gap-6 text-sm text-slate-400">
              <div className="flex items-center gap-2">
                <div className="h-2 w-2 rounded-full bg-green-500" />
                Free & Open Source
              </div>
              <div className="flex items-center gap-2">
                <div className="h-2 w-2 rounded-full bg-blue-500" />
                Enterprise Ready
              </div>
              <div className="flex items-center gap-2">
                <div className="h-2 w-2 rounded-full bg-purple-500" />
                5-Minute Setup
              </div>
            </div>

            {/* Stats section */}
            <StatsSection />
          </div>

          {/* Right column - Code example */}
          <div className="flex items-center justify-center lg:justify-end">
            <div className="w-full max-w-lg">
              <CodeExample />
              
              {/* Feature callouts */}
              <div className="mt-8 space-y-4">
                <div className="flex items-center gap-3 text-slate-300">
                  <div className="flex h-8 w-8 items-center justify-center rounded-full bg-primary-500/20">
                    <div className="h-2 w-2 rounded-full bg-primary-400" />
                  </div>
                  <span>Statistical analysis with outlier detection</span>
                </div>
                <div className="flex items-center gap-3 text-slate-300">
                  <div className="flex h-8 w-8 items-center justify-center rounded-full bg-secondary-500/20">
                    <div className="h-2 w-2 rounded-full bg-secondary-400" />
                  </div>
                  <span>Machine learning complexity estimation</span>
                </div>
                <div className="flex items-center gap-3 text-slate-300">
                  <div className="flex h-8 w-8 items-center justify-center rounded-full bg-green-500/20">
                    <div className="h-2 w-2 rounded-full bg-green-400" />
                  </div>
                  <span>Automated regression detection</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
