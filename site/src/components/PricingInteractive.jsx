import { useState } from 'react';
import { Button } from './ui/Button';
import { Card } from './ui/Card';
import { Badge } from './ui/Badge';

export function PricingInteractive() {
  const [selectedPlan, setSelectedPlan] = useState('open-source');
  const [showLicenseForm, setShowLicenseForm] = useState(false);
  const [licenseForm, setLicenseForm] = useState({
    companyName: '',
    contactEmail: '',
    contactName: '',
    companySize: '',
    useCase: ''
  });

  const plans = {
    'open-source': {
      name: 'Open Source',
      price: 'Free Forever',
      description: 'Perfect for individual developers, small teams, and open source projects.',
      features: [
        'Full Sailfish Core',
        'Statistical Analysis',
        'SailDiff Performance Comparison',
        'ScaleFish ML Analysis',
        'Test Lifecycle Management',
        'Multiple Output Formats',
        'Community Support',
        'MIT License'
      ],
      cta: 'Get Started Free',
      ctaLink: '/docs/0/getting-started',
      popular: false
    },
    'enterprise': {
      name: 'Enterprise',
      price: '$2,000/year',
      description: 'For growing companies that need advanced features and priority support.',
      features: [
        'Everything in Open Source',
        'Advanced Analytics Dashboard',
        'Team Management',
        'Custom Integrations',
        'White-label Options',
        'Compliance & Security',
        'On-premise Deployment',
        'Priority Support (4hr response)',
        'Dedicated Account Manager',
        'Custom Training',
        'Executive Dashboards'
      ],
      cta: 'Start Enterprise Trial',
      ctaLink: '#',
      popular: true
    }
  };

  const handleEnterpriseClick = () => {
    setShowLicenseForm(true);
  };

  const handleLicenseRequest = async (e) => {
    e.preventDefault();
    
    try {
      const response = await fetch('/api/licenses/generate', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          customerInfo: licenseForm,
          licenseType: 'enterprise'
        }),
      });

      const result = await response.json();
      
      if (result.success) {
        alert(`Enterprise license generated! License Key: ${result.license.licenseKey}`);
        setShowLicenseForm(false);
        setLicenseForm({
          companyName: '',
          contactEmail: '',
          contactName: '',
          companySize: '',
          useCase: ''
        });
      } else {
        alert('Failed to generate license. Please try again.');
      }
    } catch (error) {
      console.error('License generation error:', error);
      alert('Failed to generate license. Please try again.');
    }
  };

  return (
    <div className="max-w-6xl mx-auto px-4 py-12">
      <div className="text-center mb-12">
        <h1 className="text-4xl font-bold text-gray-900 dark:text-white mb-4">
          Simple, Transparent Pricing
        </h1>
        <p className="text-xl text-gray-600 dark:text-gray-400">
          Choose the plan that fits your team's needs. Start free, upgrade when you're ready to scale.
        </p>
      </div>

      <div className="grid md:grid-cols-2 gap-8 mb-12">
        {Object.entries(plans).map(([key, plan]) => (
          <Card 
            key={key}
            className={`relative p-8 ${plan.popular ? 'ring-2 ring-blue-500' : ''}`}
          >
            {plan.popular && (
              <Badge className="absolute -top-3 left-1/2 transform -translate-x-1/2 bg-blue-500 text-white">
                Most Popular
              </Badge>
            )}
            
            <div className="text-center mb-6">
              <h3 className="text-2xl font-bold text-gray-900 dark:text-white mb-2">
                {plan.name}
              </h3>
              <div className="text-3xl font-bold text-blue-600 dark:text-blue-400 mb-2">
                {plan.price}
              </div>
              <p className="text-gray-600 dark:text-gray-400">
                {plan.description}
              </p>
            </div>

            <ul className="space-y-3 mb-8">
              {plan.features.map((feature, index) => (
                <li key={index} className="flex items-center">
                  <svg className="w-5 h-5 text-green-500 mr-3" fill="currentColor" viewBox="0 0 20 20">
                    <path fillRule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clipRule="evenodd" />
                  </svg>
                  <span className="text-gray-700 dark:text-gray-300">{feature}</span>
                </li>
              ))}
            </ul>

            <Button
              className="w-full"
              variant={plan.popular ? 'default' : 'outline'}
              onClick={key === 'enterprise' ? handleEnterpriseClick : () => window.location.href = plan.ctaLink}
            >
              {plan.cta}
            </Button>
          </Card>
        ))}
      </div>

      {/* License Request Form */}
      {showLicenseForm && (
        <Card className="max-w-2xl mx-auto p-8">
          <h3 className="text-2xl font-bold text-gray-900 dark:text-white mb-6">
            Request Enterprise License
          </h3>
          <form onSubmit={handleLicenseRequest} className="space-y-4">
            <div>
              <label className="block text-sm font-medium mb-2">Company Name *</label>
              <input
                type="text"
                required
                value={licenseForm.companyName}
                onChange={(e) => setLicenseForm({...licenseForm, companyName: e.target.value})}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 dark:bg-gray-800 dark:border-gray-600 dark:text-white"
              />
            </div>
            <div>
              <label className="block text-sm font-medium mb-2">Contact Email *</label>
              <input
                type="email"
                required
                value={licenseForm.contactEmail}
                onChange={(e) => setLicenseForm({...licenseForm, contactEmail: e.target.value})}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 dark:bg-gray-800 dark:border-gray-600 dark:text-white"
              />
            </div>
            <div>
              <label className="block text-sm font-medium mb-2">Contact Name *</label>
              <input
                type="text"
                required
                value={licenseForm.contactName}
                onChange={(e) => setLicenseForm({...licenseForm, contactName: e.target.value})}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 dark:bg-gray-800 dark:border-gray-600 dark:text-white"
              />
            </div>
            <div>
              <label className="block text-sm font-medium mb-2">Company Size</label>
              <select
                value={licenseForm.companySize}
                onChange={(e) => setLicenseForm({...licenseForm, companySize: e.target.value})}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 dark:bg-gray-800 dark:border-gray-600 dark:text-white"
              >
                <option value="">Select company size</option>
                <option value="1-10">1-10 employees</option>
                <option value="11-50">11-50 employees</option>
                <option value="51-200">51-200 employees</option>
                <option value="201-1000">201-1000 employees</option>
                <option value="1000+">1000+ employees</option>
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium mb-2">Use Case</label>
              <textarea
                value={licenseForm.useCase}
                onChange={(e) => setLicenseForm({...licenseForm, useCase: e.target.value})}
                rows={3}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 dark:bg-gray-800 dark:border-gray-600 dark:text-white"
                placeholder="Tell us about your performance testing needs..."
              />
            </div>
            <div className="flex gap-4">
              <Button type="submit" className="flex-1">
                Generate License
              </Button>
              <Button 
                type="button" 
                variant="outline" 
                onClick={() => setShowLicenseForm(false)}
              >
                Cancel
              </Button>
            </div>
          </form>
        </Card>
      )}
    </div>
  );
}
