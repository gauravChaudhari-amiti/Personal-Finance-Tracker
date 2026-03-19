import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { authService } from "../services/authService";
import { useAuthStore } from "../store/authStore";
import ThemeToggle from "../components/ThemeToggle";
import { consumeSessionNotice } from "../utils/sessionTimeout";

export default function LoginPage() {
  const navigate = useNavigate();
  const setUser = useAuthStore((state) => state.setUser);

  const [mode, setMode] = useState<"login" | "register">("login");
  const [displayName, setDisplayName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [error, setError] = useState(consumeSessionNotice());
  const [loading, setLoading] = useState(false);
  const displayNameId = "auth-display-name";
  const emailId = "auth-email";
  const passwordId = "auth-password";
  const confirmPasswordId = "auth-confirm-password";

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");

    try {
      if (mode === "register" && password !== confirmPassword) {
        setError("Passwords do not match.");
        return;
      }

      setLoading(true);

      const data =
        mode === "login"
          ? await authService.login({ email, password })
          : await authService.register({ displayName, email, password });

      setUser(data);
      navigate("/dashboard");
    } catch (err: any) {
      setError(
        err?.response?.data?.message ||
          (mode === "login" ? "Login failed." : "Account creation failed.")
      );
    } finally {
      setLoading(false);
    }
  };

  const switchMode = (nextMode: "login" | "register") => {
    setMode(nextMode);
    setError("");
    setPassword("");
    setConfirmPassword("");
  };

  return (
    <div className="auth-page">
      <div className="auth-card">
        <div className="auth-header">
          <h1>Personal Finance Tracker</h1>
          <ThemeToggle />
        </div>

        <div className="auth-switch" role="tablist" aria-label="Authentication mode">
          <button
            type="button"
            className={`auth-switch-btn ${mode === "login" ? "active" : ""}`}
            onClick={() => switchMode("login")}
          >
            Log In
          </button>
          <button
            type="button"
            className={`auth-switch-btn ${mode === "register" ? "active" : ""}`}
            onClick={() => switchMode("register")}
          >
            Create Account
          </button>
        </div>

        <p>
          {mode === "login"
            ? "Log in to your account to continue."
            : "Create your account to start tracking your finances."}
        </p>

        {error && (
          <div className="error-text" role="alert" aria-live="polite" data-testid="auth-error">
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} data-testid="auth-form">
          {mode === "register" && (
            <div className="form-group">
              <label htmlFor={displayNameId}>Display Name</label>
              <input
                id={displayNameId}
                value={displayName}
                onChange={(e) => setDisplayName(e.target.value)}
                placeholder="Enter your name"
                autoComplete="name"
                data-testid="auth-display-name"
              />
            </div>
          )}

          <div className="form-group">
            <label htmlFor={emailId}>Email</label>
            <input
              id={emailId}
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="Enter email"
              autoComplete="email"
              data-testid="auth-email"
            />
          </div>

          <div className="form-group">
            <label htmlFor={passwordId}>Password</label>
            <input
              id={passwordId}
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="Enter password"
              autoComplete={mode === "login" ? "current-password" : "new-password"}
              data-testid="auth-password"
            />
          </div>

          {mode === "register" && (
            <div className="form-group">
              <label htmlFor={confirmPasswordId}>Confirm Password</label>
              <input
                id={confirmPasswordId}
                type="password"
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                placeholder="Confirm password"
                autoComplete="new-password"
                data-testid="auth-confirm-password"
              />
            </div>
          )}

          <button type="submit" className="primary-btn" disabled={loading} data-testid="auth-submit">
            {loading
              ? mode === "login"
                ? "Logging in..."
                : "Creating account..."
              : mode === "login"
                ? "Log In"
                : "Create Account"}
          </button>
        </form>
      </div>
    </div>
  );
}
