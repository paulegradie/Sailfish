import { Layout } from '@/components/Layout';
import { PricingInteractive } from '@/components/PricingInteractive';

export default function PricingInteractivePage() {
  return (
    <Layout
      title="Pricing - Sailfish Performance Testing"
      description="Simple, transparent pricing that scales with your success. Start free, upgrade when you're ready to scale."
    >
      <div className="min-h-screen bg-gray-50 dark:bg-gray-900">
        <PricingInteractive />
        
        {/* FAQ Section */}
        <div className="max-w-4xl mx-auto px-4 py-12">
          <h2 className="text-3xl font-bold text-center text-gray-900 dark:text-white mb-12">
            Frequently Asked Questions
          </h2>
          
          <div className="space-y-8">
            <div>
              <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-2">
                💼 Who needs an Enterprise license?
              </h3>
              <p className="text-gray-600 dark:text-gray-400">
                Companies with annual recurring revenue (ARR) exceeding $5 million are required to purchase an Enterprise license to use Sailfish in commercial applications. This ensures we can continue investing in the platform while providing the enterprise-grade support your business needs.
              </p>
            </div>
            
            <div>
              <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-2">
                🚀 What's included in Enterprise support?
              </h3>
              <p className="text-gray-600 dark:text-gray-400">
                Enterprise customers receive priority email support with guaranteed 4-hour response times, direct access to our engineering team via phone and video calls, custom training sessions, implementation assistance, and a dedicated account manager as your single point of contact.
              </p>
            </div>
            
            <div>
              <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-2">
                📈 Can I upgrade from Open Source to Enterprise?
              </h3>
              <p className="text-gray-600 dark:text-gray-400">
                Absolutely! You can upgrade at any time with zero downtime. Your existing tests and data will seamlessly transfer to the Enterprise platform. Use the form above to generate an Enterprise license immediately.
              </p>
            </div>
            
            <div>
              <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-2">
                🔒 Is my data secure with Sailfish?
              </h3>
              <p className="text-gray-600 dark:text-gray-400">
                Security is our top priority. Enterprise customers get SOC 2 Type II compliance, GDPR compliance features, on-premise deployment options, encrypted data transmission and storage, and regular security audits.
              </p>
            </div>
            
            <div>
              <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-2">
                📊 What kind of ROI can we expect?
              </h3>
              <p className="text-gray-600 dark:text-gray-400">
                Our Enterprise customers typically see 50-75% reduction in performance testing setup time, 40-60% faster identification of performance bottlenecks, and 25-40% improvement in application performance after optimization.
              </p>
            </div>
          </div>
        </div>
        
        {/* CTA Section */}
        <div className="bg-blue-600 dark:bg-blue-800">
          <div className="max-w-4xl mx-auto px-4 py-16 text-center">
            <h2 className="text-3xl font-bold text-white mb-4">
              Ready to Get Started?
            </h2>
            <p className="text-xl text-blue-100 mb-8">
              Join thousands of developers who trust Sailfish for their performance testing needs.
            </p>
            <div className="flex gap-4 justify-center flex-wrap">
              <a
                href="/docs/0/getting-started"
                className="bg-white text-blue-600 px-8 py-4 rounded-lg font-semibold text-lg hover:bg-gray-100 transition-colors"
              >
                Try Free Now →
              </a>
              <a
                href="/license-manager"
                className="border-2 border-white text-white px-8 py-4 rounded-lg font-semibold text-lg hover:bg-white hover:text-blue-600 transition-colors"
              >
                Manage License →
              </a>
            </div>
            <p className="text-blue-100 text-sm mt-4">
              No credit card required • 5-minute setup • Full feature access
            </p>
          </div>
        </div>
      </div>
    </Layout>
  );
}
