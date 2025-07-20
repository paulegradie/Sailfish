// Enterprise features detection and gating system
// This would be integrated into the Sailfish library

class EnterpriseFeatureManager {
  constructor() {
    this.licenseCache = null;
    this.cacheExpiry = null;
    this.validationEndpoint = '/api/licenses/validate';
  }

  // Check if a specific feature is available
  async hasFeature(featureName) {
    const license = await this.validateLicense();
    if (!license || !license.valid) {
      return false;
    }
    
    return license.license?.features?.[featureName] === true;
  }

  // Validate license with caching
  async validateLicense(licenseKey = null) {
    // Use provided key or try to get from environment/config
    const keyToValidate = licenseKey || this.getLicenseKey();
    
    if (!keyToValidate) {
      return { valid: false, error: 'No license key provided' };
    }

    // Check cache first
    if (this.licenseCache && this.cacheExpiry && Date.now() < this.cacheExpiry) {
      return this.licenseCache;
    }

    try {
      const response = await fetch(this.validationEndpoint, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ licenseKey: keyToValidate }),
      });

      const result = await response.json();
      
      // Cache valid results for 1 hour
      if (result.valid) {
        this.licenseCache = result;
        this.cacheExpiry = Date.now() + (60 * 60 * 1000); // 1 hour
      }

      return result;
    } catch (error) {
      console.error('License validation failed:', error);
      return { valid: false, error: 'Validation request failed' };
    }
  }

  // Get license key from various sources
  getLicenseKey() {
    // Try environment variable first
    if (typeof process !== 'undefined' && process.env?.SAILFISH_LICENSE_KEY) {
      return process.env.SAILFISH_LICENSE_KEY;
    }

    // Try local storage (browser)
    if (typeof window !== 'undefined' && window.localStorage) {
      return window.localStorage.getItem('sailfish_license_key');
    }

    // Try configuration file or other sources
    return null;
  }

  // Set license key
  setLicenseKey(licenseKey) {
    if (typeof window !== 'undefined' && window.localStorage) {
      window.localStorage.setItem('sailfish_license_key', licenseKey);
    }
    
    // Clear cache to force revalidation
    this.licenseCache = null;
    this.cacheExpiry = null;
  }

  // Feature-specific checks
  async canUseScaleFish() {
    return await this.hasFeature('scalefish');
  }

  async canUseSailDiff() {
    return await this.hasFeature('saildiff');
  }

  async hasPrioritySupport() {
    return await this.hasFeature('prioritySupport');
  }

  async hasEnterpriseFeatures() {
    return await this.hasFeature('enterpriseFeatures');
  }

  // Get license information
  async getLicenseInfo() {
    const license = await this.validateLicense();
    if (!license.valid) {
      return null;
    }

    return {
      licenseType: license.license.licenseType,
      companyName: license.license.customerInfo?.companyName,
      expiresAt: license.license.expiresAt,
      features: license.license.features,
      status: license.license.status
    };
  }

  // Clear license cache
  clearCache() {
    this.licenseCache = null;
    this.cacheExpiry = null;
  }
}

// Singleton instance
const enterpriseFeatures = new EnterpriseFeatureManager();

// Feature gating decorator/wrapper
export function requiresEnterpriseLicense(featureName) {
  return function(target, propertyKey, descriptor) {
    const originalMethod = descriptor.value;
    
    descriptor.value = async function(...args) {
      const hasFeature = await enterpriseFeatures.hasFeature(featureName);
      
      if (!hasFeature) {
        throw new Error(
          `This feature (${featureName}) requires an enterprise license. ` +
          'Please visit https://sailfish.dev/pricing to upgrade.'
        );
      }
      
      return originalMethod.apply(this, args);
    };
    
    return descriptor;
  };
}

// Usage tracking for enterprise features
export class UsageTracker {
  constructor() {
    this.usageData = new Map();
  }

  trackFeatureUsage(featureName, metadata = {}) {
    const key = `${featureName}_${new Date().toISOString().split('T')[0]}`;
    const current = this.usageData.get(key) || { count: 0, metadata: [] };
    
    this.usageData.set(key, {
      count: current.count + 1,
      metadata: [...current.metadata, { timestamp: new Date().toISOString(), ...metadata }]
    });
  }

  getUsageReport(days = 30) {
    const cutoff = new Date();
    cutoff.setDate(cutoff.getDate() - days);
    
    const report = {};
    for (const [key, data] of this.usageData.entries()) {
      const [feature, date] = key.split('_');
      if (new Date(date) >= cutoff) {
        if (!report[feature]) {
          report[feature] = { totalUsage: 0, dailyUsage: {} };
        }
        report[feature].totalUsage += data.count;
        report[feature].dailyUsage[date] = data.count;
      }
    }
    
    return report;
  }
}

export { enterpriseFeatures };
export default EnterpriseFeatureManager;
