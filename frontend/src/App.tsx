import React, { useState } from 'react';
import './App.css';
import './components/styles.css';
import Login from './components/Auth/Login';
import Register from './components/Auth/Register';
import CurrencyConverter from './components/CurrencyConverter/CurrencyConverter';
import ForgotPassword from './components/ForgotPassword';
import CurrencyRates from './components/CurrencyRates';

function App() {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [showLogin, setShowLogin] = useState(true);
  const [showForgotPassword, setShowForgotPassword] = useState(false);
  const [showCurrencyRates, setShowCurrencyRates] = useState(false);

  const handleLoginSuccess = () => {
    setIsAuthenticated(true);
  };

  const handleRegisterSuccess = () => {
    setShowLogin(true);
  };

  const handleLogout = () => {
    localStorage.removeItem('token');
    setIsAuthenticated(false);
  };

  return (
    <div className="App">
      <header className="App-header">
        <h1>Santander</h1>
        {isAuthenticated && (
          <button onClick={handleLogout} className="logout-button">
            Logout
          </button>
        )}
        {isAuthenticated && (
          <button onClick={() => setShowCurrencyRates(true)}>
            Currency Rates
          </button>
        )}
      </header>
      <main>
        {!isAuthenticated ? (
          <div className="auth-container">
            {showLogin ? (
              <>
                <Login onLoginSuccess={handleLoginSuccess} />
                <button onClick={() => setShowLogin(false)}>
                 Register
                </button>
                <button 
                  className="forgot-password-link"
                  onClick={() => setShowForgotPassword(true)}
                >
                  Forgot Password?
                </button>
              </>
            ) : (
              <>
                <Register onRegisterSuccess={handleRegisterSuccess} />
                <button onClick={() => setShowLogin(true)}>
                  Back to Login
                </button>
              </>
            )}
          </div>
        ) : (
          <CurrencyConverter />
        )}

        {showForgotPassword && (
          <ForgotPassword onClose={() => setShowForgotPassword(false)} />
        )}

        {showCurrencyRates && (
          <CurrencyRates />
        )}
      </main>
    </div>
  );
}

export default App; 