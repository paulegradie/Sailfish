import { useState } from 'react';
import { Button } from './ui/Button';
import { Card } from './ui/Card';
import { Badge } from './ui/Badge';

export function LicenseManager() {
  const [licenseKey, setLicenseKey] = useState('');
  const [validationResult, setValidationResult] = useState(null);
  const [isValidating, setIsValidating] = useState(false);
  const [generatedLicense, setGeneratedLicense] = useState(null);

  const validateLicense = async () => {
    if (!licenseKey.trim()) return;
    
    setIsValidating(true);
    try {
      const response = await fetch('/api/licenses/validate', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ licenseKey: licenseKey.trim() }),
      });

      const result = await response.json();
      setValidationResult(result);
    } catch (error) {
      setValidationResult({
        valid: false,
        error: 'Failed to validate license',
        code: 'NETWORK_ERROR'
      });
    } finally {
      setIsValidating(false);
    }
  };

  const generateTestLicense = async () => {
    try {
      const response = await fetch('/api/licenses/generate', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          customerInfo: {
            companyName: 'Test Company',
            contactEmail: 'test@example.com',
            contactName: 'Test User'
          },
          licenseType: 'enterprise'
        }),
      });

      const result = await response.json();
      if (result.success) {
        setGeneratedLicense(result.license);
        setLicenseKey(result.license.licenseKey);
      }
    } catch (error) {
      console.error('Failed to generate test license:', error);
    }
  };

  return (
    <div className="max-w-4xl mx-auto p-6 space-y-6">
      <div className="text-center">
        <h1 className="text-3xl font-bold text-gray-900 dark:text-white mb-2">
          License Management
        </h1>
        <p className="text-gray-600 dark:text-gray-400">
          Validate and manage Sailfish enterprise licenses
        </p>
      </div>

      {/* License Validation Section */}
      <Card className="p-6">
        <h2 className="text-xl font-semibold mb-4">Validate License</h2>
        <div className="space-y-4">
          <div>
            <label htmlFor="licenseKey" className="block text-sm font-medium mb-2">
              License Key
            </label>
            <input
              id="licenseKey"
              type="text"
              value={licenseKey}
              onChange={(e) => setLicenseKey(e.target.value)}
              placeholder="XXXXXXXX-XXXXXXXX-XXXXXXXX-XXXXXXXX"
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 dark:bg-gray-800 dark:border-gray-600 dark:text-white"
            />
          </div>
          <div className="flex gap-2">
            <Button 
              onClick={validateLicense} 
              disabled={isValidating || !licenseKey.trim()}
              className="flex-1"
            >
              {isValidating ? 'Validating...' : 'Validate License'}
            </Button>
            <Button 
              onClick={generateTestLicense}
              variant="outline"
            >
              Generate Test License
            </Button>
          </div>
        </div>

        {/* Validation Results */}
        {validationResult && (
          <div className="mt-6 p-4 rounded-lg border">
            <div className="flex items-center gap-2 mb-2">
              <Badge variant={validationResult.valid ? 'success' : 'destructive'}>
                {validationResult.valid ? 'Valid' : 'Invalid'}
              </Badge>
              {validationResult.license?.licenseType && (
                <Badge variant="secondary">
                  {validationResult.license.licenseType}
                </Badge>
              )}
            </div>
            
            {validationResult.valid ? (
              <div className="space-y-2 text-sm">
                <p><strong>Company:</strong> {validationResult.license.customerInfo?.companyName}</p>
                <p><strong>Expires:</strong> {new Date(validationResult.license.expiresAt).toLocaleDateString()}</p>
                <p><strong>Features:</strong></p>
                <ul className="list-disc list-inside ml-4">
                  {Object.entries(validationResult.license.features || {}).map(([feature, enabled]) => (
                    <li key={feature} className={enabled ? 'text-green-600' : 'text-gray-500'}>
                      {feature}: {enabled ? 'Enabled' : 'Disabled'}
                    </li>
                  ))}
                </ul>
              </div>
            ) : (
              <div className="text-sm text-red-600">
                <p><strong>Error:</strong> {validationResult.error}</p>
                <p><strong>Code:</strong> {validationResult.code}</p>
              </div>
            )}
          </div>
        )}
      </Card>

      {/* Generated License Display */}
      {generatedLicense && (
        <Card className="p-6">
          <h2 className="text-xl font-semibold mb-4">Generated Test License</h2>
          <div className="bg-gray-50 dark:bg-gray-800 p-4 rounded-lg">
            <pre className="text-sm overflow-x-auto">
              {JSON.stringify(generatedLicense, null, 2)}
            </pre>
          </div>
        </Card>
      )}
    </div>
  );
}
