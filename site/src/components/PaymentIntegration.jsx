import { useState } from 'react';
import { Button } from './ui/Button';
import { Card } from './ui/Card';

// Mock Stripe integration - replace with actual Stripe implementation
export function PaymentIntegration({ plan, onSuccess, onError }) {
  const [isProcessing, setIsProcessing] = useState(false);
  const [paymentForm, setPaymentForm] = useState({
    email: '',
    cardNumber: '',
    expiryDate: '',
    cvv: '',
    name: '',
    company: '',
    address: '',
    city: '',
    country: '',
    zipCode: ''
  });

  const handlePayment = async (e) => {
    e.preventDefault();
    setIsProcessing(true);

    try {
      // Mock payment processing - replace with actual Stripe integration
      await new Promise(resolve => setTimeout(resolve, 2000));

      // Simulate successful payment
      const paymentResult = {
        success: true,
        paymentId: `pay_${Math.random().toString(36).substr(2, 9)}`,
        amount: plan.price === '$2,000/year' ? 200000 : 0, // in cents
        currency: 'usd',
        customer: {
          email: paymentForm.email,
          name: paymentForm.name,
          company: paymentForm.company
        }
      };

      // Generate license after successful payment
      const licenseResponse = await fetch('/api/licenses/generate', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          customerInfo: {
            companyName: paymentForm.company,
            contactEmail: paymentForm.email,
            contactName: paymentForm.name
          },
          licenseType: 'enterprise',
          paymentId: paymentResult.paymentId
        }),
      });

      const licenseResult = await licenseResponse.json();

      if (licenseResult.success) {
        onSuccess({
          payment: paymentResult,
          license: licenseResult.license
        });
      } else {
        throw new Error('Failed to generate license after payment');
      }

    } catch (error) {
      console.error('Payment error:', error);
      onError(error.message || 'Payment processing failed');
    } finally {
      setIsProcessing(false);
    }
  };

  return (
    <Card className="max-w-2xl mx-auto p-8">
      <div className="text-center mb-8">
        <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-2">
          Complete Your Purchase
        </h2>
        <p className="text-gray-600 dark:text-gray-400">
          {plan.name} - {plan.price}
        </p>
      </div>

      <form onSubmit={handlePayment} className="space-y-6">
        {/* Customer Information */}
        <div>
          <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
            Customer Information
          </h3>
          <div className="grid md:grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium mb-2">Email *</label>
              <input
                type="email"
                required
                value={paymentForm.email}
                onChange={(e) => setPaymentForm({...paymentForm, email: e.target.value})}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 dark:bg-gray-800 dark:border-gray-600 dark:text-white"
              />
            </div>
            <div>
              <label className="block text-sm font-medium mb-2">Full Name *</label>
              <input
                type="text"
                required
                value={paymentForm.name}
                onChange={(e) => setPaymentForm({...paymentForm, name: e.target.value})}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 dark:bg-gray-800 dark:border-gray-600 dark:text-white"
              />
            </div>
            <div className="md:col-span-2">
              <label className="block text-sm font-medium mb-2">Company Name *</label>
              <input
                type="text"
                required
                value={paymentForm.company}
                onChange={(e) => setPaymentForm({...paymentForm, company: e.target.value})}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 dark:bg-gray-800 dark:border-gray-600 dark:text-white"
              />
            </div>
          </div>
        </div>

        {/* Payment Information */}
        <div>
          <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
            Payment Information
          </h3>
          <div className="space-y-4">
            <div>
              <label className="block text-sm font-medium mb-2">Card Number *</label>
              <input
                type="text"
                required
                placeholder="1234 5678 9012 3456"
                value={paymentForm.cardNumber}
                onChange={(e) => setPaymentForm({...paymentForm, cardNumber: e.target.value})}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 dark:bg-gray-800 dark:border-gray-600 dark:text-white"
              />
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium mb-2">Expiry Date *</label>
                <input
                  type="text"
                  required
                  placeholder="MM/YY"
                  value={paymentForm.expiryDate}
                  onChange={(e) => setPaymentForm({...paymentForm, expiryDate: e.target.value})}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 dark:bg-gray-800 dark:border-gray-600 dark:text-white"
                />
              </div>
              <div>
                <label className="block text-sm font-medium mb-2">CVV *</label>
                <input
                  type="text"
                  required
                  placeholder="123"
                  value={paymentForm.cvv}
                  onChange={(e) => setPaymentForm({...paymentForm, cvv: e.target.value})}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 dark:bg-gray-800 dark:border-gray-600 dark:text-white"
                />
              </div>
            </div>
          </div>
        </div>

        {/* Billing Address */}
        <div>
          <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
            Billing Address
          </h3>
          <div className="space-y-4">
            <div>
              <label className="block text-sm font-medium mb-2">Address *</label>
              <input
                type="text"
                required
                value={paymentForm.address}
                onChange={(e) => setPaymentForm({...paymentForm, address: e.target.value})}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 dark:bg-gray-800 dark:border-gray-600 dark:text-white"
              />
            </div>
            <div className="grid md:grid-cols-3 gap-4">
              <div>
                <label className="block text-sm font-medium mb-2">City *</label>
                <input
                  type="text"
                  required
                  value={paymentForm.city}
                  onChange={(e) => setPaymentForm({...paymentForm, city: e.target.value})}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 dark:bg-gray-800 dark:border-gray-600 dark:text-white"
                />
              </div>
              <div>
                <label className="block text-sm font-medium mb-2">Country *</label>
                <select
                  required
                  value={paymentForm.country}
                  onChange={(e) => setPaymentForm({...paymentForm, country: e.target.value})}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 dark:bg-gray-800 dark:border-gray-600 dark:text-white"
                >
                  <option value="">Select Country</option>
                  <option value="US">United States</option>
                  <option value="CA">Canada</option>
                  <option value="GB">United Kingdom</option>
                  <option value="DE">Germany</option>
                  <option value="FR">France</option>
                  <option value="AU">Australia</option>
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium mb-2">ZIP Code *</label>
                <input
                  type="text"
                  required
                  value={paymentForm.zipCode}
                  onChange={(e) => setPaymentForm({...paymentForm, zipCode: e.target.value})}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 dark:bg-gray-800 dark:border-gray-600 dark:text-white"
                />
              </div>
            </div>
          </div>
        </div>

        {/* Submit Button */}
        <div className="pt-6">
          <Button
            type="submit"
            disabled={isProcessing}
            className="w-full text-lg py-4"
          >
            {isProcessing ? 'Processing Payment...' : `Pay ${plan.price}`}
          </Button>
          <p className="text-sm text-gray-500 text-center mt-4">
            🔒 Your payment information is secure and encrypted
          </p>
        </div>
      </form>
    </Card>
  );
}
