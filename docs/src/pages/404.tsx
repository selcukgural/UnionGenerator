import React from 'react';
import Layout from '@theme/Layout';
import Link from '@docusaurus/Link';

export default function NotFound() {
  return (
    <Layout
      title="Page Not Found"
      description="The page you are looking for does not exist.">
      <main className="container margin-vert--xl">
        <div className="row">
          <div className="col col--6 col--offset-3">
            <h1 className="hero__title">
              404 - Page Not Found
            </h1>
            <p className="hero__subtitle">
              Oops! The page you're looking for doesn't exist.
            </p>
            <p>
              It seems you've hit a broken link or the page has been moved.
              Don't worry, you can find your way back:
            </p>
            <div className="margin-vert--lg">
              <Link
                className="button button--primary button--lg"
                to="/">
                🏠 Go to Home
              </Link>
              {' '}
              <Link
                className="button button--secondary button--lg"
                to="/getting-started/quick-start">
                🚀 Getting Started
              </Link>
            </div>
            <div className="margin-top--lg">
              <h3>Popular Pages</h3>
              <ul>
                <li>
                  <Link to="/introduction/what-is-uniongenerator">
                    What is UnionGenerator?
                  </Link>
                </li>
                <li>
                  <Link to="/getting-started/quick-start">
                    Quick Start Guide
                  </Link>
                </li>
                <li>
                  <Link to="/core-package/overview">
                    Core Package Documentation
                  </Link>
                </li>
                <li>
                  <Link to="/api-reference/overview">
                    API Reference
                  </Link>
                </li>
              </ul>
            </div>
            <div className="margin-top--lg">
              <p>
                <strong>Need help?</strong> Check out our{' '}
                <a href="https://github.com/selcukgural/UnionGenerator/issues">
                  GitHub Issues
                </a>{' '}
                or{' '}
                <a href="https://github.com/selcukgural/UnionGenerator/discussions">
                  Discussions
                </a>
                .
              </p>
            </div>
          </div>
        </div>
      </main>
    </Layout>
  );
}
