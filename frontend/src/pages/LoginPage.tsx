import { useEffect, useMemo, useState } from "react";
import { Navigate, useNavigate, useSearchParams } from "react-router-dom";
import ThemeToggle from "../components/ThemeToggle";
import { authService } from "../services/authService";
import { useAuthStore } from "../store/authStore";
import { consumeSessionNotice } from "../utils/sessionTimeout";

type AuthMode = "login" | "register" | "forgot";

export default function LoginPage() {
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();
  const { user, isAuthResolved } = useAuthStore((state) => ({
    user: state.user,
    isAuthResolved: state.isAuthResolved
  }));
  const setUser = useAuthStore((state) => state.setUser);

  const requestedMode = useMemo<AuthMode>(() => {
    const modeParam = searchParams.get("mode");
    return modeParam === "register" || modeParam === "forgot" ? modeParam : "login";
  }, [searchParams]);

  const [mode, setMode] = useState<AuthMode>(requestedMode);
  const [displayName, setDisplayName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [error, setError] = useState(consumeSessionNotice());
  const [notice, setNotice] = useState("");
  const [previewUrl, setPreviewUrl] = useState<string | undefined>();
  const [pendingVerificationEmail, setPendingVerificationEmail] = useState("");
  const [loading, setLoading] = useState(false);
  const displayNameId = "auth-display-name";
  const emailId = "auth-email";
  const passwordId = "auth-password";
  const confirmPasswordId = "auth-confirm-password";

  const clearMessages = () => {
    setError("");
    setNotice("");
    setPreviewUrl(undefined);
  };

  useEffect(() => {
    setMode(requestedMode);
  }, [requestedMode]);

  const switchMode = (nextMode: AuthMode) => {
    setMode(nextMode);
    setSearchParams(nextMode === "login" ? {} : { mode: nextMode });
    clearMessages();
    setPassword("");
    setConfirmPassword("");
  };

  const handleResendVerification = async () => {
    const normalizedEmail = pendingVerificationEmail || email.trim();
    if (!normalizedEmail) {
      setError("Enter your email first so we know where to resend the verification link.");
      return;
    }

    try {
      clearMessages();
      setLoading(true);
      const result = await authService.resendVerification({ email: normalizedEmail });
      setNotice(result.message);
      setPreviewUrl(result.previewUrl);
    } catch (err: any) {
      setError(err?.response?.data?.message || "Failed to resend verification email.");
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    clearMessages();

    const normalizedDisplayName = displayName.trim();
    const normalizedEmail = email.trim();

    if (mode === "register" && !normalizedDisplayName) {
      setError("Display name is required.");
      return;
    }

    if (!normalizedEmail) {
      setError("Email is required.");
      return;
    }

    if (mode !== "forgot" && !password) {
      setError("Password is required.");
      return;
    }

    try {
      if (mode === "register" && password !== confirmPassword) {
        setError("Passwords do not match.");
        return;
      }

      setLoading(true);

      if (mode === "login") {
        const data = await authService.login({ email: normalizedEmail, password });
        setUser(data);
        navigate("/dashboard");
        return;
      }

      if (mode === "register") {
        const result = await authService.register({
          displayName: normalizedDisplayName,
          email: normalizedEmail,
          password
        });

        setPendingVerificationEmail(normalizedEmail);
        setNotice(result.message);
        setPreviewUrl(result.previewUrl);
        setDisplayName("");
        setEmail(normalizedEmail);
        setPassword("");
        setConfirmPassword("");
        return;
      }

      const result = await authService.forgotPassword({ email: normalizedEmail });
      setNotice(result.message);
      setPreviewUrl(result.previewUrl);
    } catch (err: any) {
      const backendMessage = err?.response?.data?.message;
      if (mode === "login" && backendMessage === "Verify your email before logging in.") {
        setPendingVerificationEmail(normalizedEmail);
      }

      setError(
        backendMessage ||
          (mode === "login"
            ? "Login failed."
            : mode === "register"
              ? "Account creation failed."
              : "Password reset request failed.")
      );
    } finally {
      setLoading(false);
    }
  };

  if (!isAuthResolved) {
    return <div className="auth-page">Loading session...</div>;
  }

  if (user) {
    return <Navigate to="/dashboard" replace />;
  }

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
            Sign Up
          </button>
        </div>

        <p>
          {mode === "login"
            ? "Log in to your account to continue."
            : mode === "register"
              ? "Create your account, then verify your email before logging in."
              : "Enter your email and we will send you a password reset link."}
        </p>

        {error && (
          <div className="error-text" role="alert" aria-live="polite" data-testid="auth-error">
            {error}
          </div>
        )}

        {notice && (
          <div className="notice-banner success" role="status" aria-live="polite">
            {notice}
            {previewUrl && (
              <div className="auth-preview-link">
                Preview link:{" "}
                <a href={previewUrl} target="_blank" rel="noreferrer">
                  Open link
                </a>
              </div>
            )}
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
                required
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
              required
            />
          </div>

          {mode !== "forgot" && (
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
                required
              />
            </div>
          )}

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
                required
              />
            </div>
          )}

          <button type="submit" className="primary-btn" disabled={loading} data-testid="auth-submit">
            {loading
              ? mode === "login"
                ? "Logging in..."
                : mode === "register"
                  ? "Creating account..."
                  : "Sending reset link..."
              : mode === "login"
                ? "Log In"
                : mode === "register"
                  ? "Create Account"
                  : "Send Reset Link"}
          </button>
        </form>

        {mode === "login" && (
          <>
            <div className="auth-link-row">
              <button type="button" className="auth-link-btn" onClick={() => switchMode("forgot")}>
                Forgot password?
              </button>
              <button type="button" className="auth-link-btn" onClick={() => switchMode("register")}>
                Don&apos;t have an account? Sign up
              </button>
            </div>
            {pendingVerificationEmail && (
              <div className="auth-link-row single">
                <button type="button" className="auth-link-btn" onClick={handleResendVerification}>
                  Resend email verification link
                </button>
              </div>
            )}
          </>
        )}

        {mode === "register" && (
          <>
            <div className="auth-link-row single">
              <button type="button" className="auth-link-btn" onClick={handleResendVerification}>
                Resend email verification link
              </button>
            </div>
            <div className="auth-link-row single">
              <button type="button" className="auth-link-btn" onClick={() => switchMode("login")}>
                Already have an account? Log in
              </button>
            </div>
          </>
        )}

        {mode === "forgot" && (
          <div className="auth-link-row single">
            <button type="button" className="auth-link-btn" onClick={() => switchMode("login")}>
              Back to log in
            </button>
          </div>
        )}
      </div>
    </div>
  );
}
