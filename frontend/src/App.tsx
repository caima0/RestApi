import React, { useState } from 'react';
import './App.css';
import Login from './components/Auth/Login';
import Register from './components/Auth/Register';
import CurrencyConverter from './components/CurrencyConverter/CurrencyConverter';

function App() {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [showLogin, setShowLogin] = useState(true);

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
        <h1>Currency Converter</h1>
        {isAuthenticated && (
          <button onClick={handleLogout} className="logout-button">
            Logout
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
                  Need an account? Register
                </button>
              </>
            ) : (
              <>
                <Register onRegisterSuccess={handleRegisterSuccess} />
                <button onClick={() => setShowLogin(true)}>
                  Already have an account? Login
                </button>
              </>
            )}
          </div>
        ) : (
          <CurrencyConverter />
        )}
      </main>
    </div>
  );
}

export default App; 