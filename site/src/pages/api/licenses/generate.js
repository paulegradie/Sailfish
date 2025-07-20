// API endpoint for generating enterprise licenses
import crypto from 'crypto';

// License generation utilities
const generateLicenseKey = () => {
  const segments = [];
  for (let i = 0; i < 4; i++) {
    segments.push(crypto.randomBytes(4).toString('hex').toUpperCase());
  }
  return segments.join('-');
};

const createLicenseData = (customerInfo, licenseType = 'enterprise') => {
  const now = new Date();
  const expiryDate = new Date(now.getFullYear() + 1, now.getMonth(), now.getDate());
  
  return {
    licenseKey: generateLicenseKey(),
    customerInfo: {
      companyName: customerInfo.companyName,
      contactEmail: customerInfo.contactEmail,
      contactName: customerInfo.contactName,
    },
    licenseType,
    features: {
      scalefish: true,
      saildiff: true,
      prioritySupport: true,
      enterpriseFeatures: true,
      maxUsers: licenseType === 'enterprise' ? -1 : 5, // -1 = unlimited
    },
    issuedAt: now.toISOString(),
    expiresAt: expiryDate.toISOString(),
    status: 'active',
    version: '1.0',
  };
};

export default async function handler(req, res) {
  if (req.method !== 'POST') {
    return res.status(405).json({ error: 'Method not allowed' });
  }

  try {
    const { customerInfo, licenseType } = req.body;

    // Validate required fields
    if (!customerInfo?.companyName || !customerInfo?.contactEmail) {
      return res.status(400).json({ 
        error: 'Missing required fields: companyName and contactEmail' 
      });
    }

    // Generate license
    const licenseData = createLicenseData(customerInfo, licenseType);

    // In a real implementation, you would:
    // 1. Save to database
    // 2. Send confirmation email
    // 3. Integrate with payment processor
    // 4. Generate signed license file

    // For now, return the license data
    res.status(200).json({
      success: true,
      license: licenseData,
      message: 'License generated successfully'
    });

  } catch (error) {
    console.error('License generation error:', error);
    res.status(500).json({ 
      error: 'Internal server error',
      message: 'Failed to generate license'
    });
  }
}
