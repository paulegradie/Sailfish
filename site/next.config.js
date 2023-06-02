const withMarkdoc = require('@markdoc/next.js')

const isProd = process.env.NODE_ENV === 'production'


/** @type {import('next').NextConfig} */
const nextConfig = {
  basePath: isProd ? "/Sailfish" : "",
  assetPrefix: isProd ? "/Sailfish" : "",
  reactStrictMode: true,
  pageExtensions: ['js', 'jsx', 'md'],
  experimental: {
    scrollRestoration: true,
  },
  webpack(config) {
    config.experiments = { ...config.experiments, topLevelAwait: true }
    return config
  },
  images: {
    loader: 'akamai',
    path: '',
  },
}

module.exports = withMarkdoc()(nextConfig)
