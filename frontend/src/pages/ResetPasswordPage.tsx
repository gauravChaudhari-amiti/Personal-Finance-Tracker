import { useMemo, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import ThemeToggle from "../components/ThemeToggle";
import { authService } from "../services/authService";

export default function ResetPasswordPage() {
  const [searchParams] = useSearchParams();
  const token = useMemo(() => searchParams.get("token") || "", [searchParams]);
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const [loading, setLoading] = useState(false);
  const hasCompletedReset = notice.length > 0;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setNotice("");

    if (!token) {
      setError("This reset link is missing a token.");
      return;
    }

    if (!password) {
      setError("Password is required.");
      return;
    }

    if (password !== confirmPassword) {
      setError("Passwords do not match.");
      return;
    }

    try {
      setLoading(true);
      const result = await authService.resetPassword({ token, password });
      setNotice(result.message);
    } catch (err: any) {
      setError(err?.response?.data?.message || "Failed to reset password.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="auth-page">
      <div className="auth-card">
        <div className="auth-header">
          <h1>Reset Password</h1>
          <ThemeToggle />
        </div>

        <p>Create a new password for your account.</p>

        {error && (
          <div className="error-text" role="alert">
            {error}
          </div>
        )}

        {notice && (
          <div className="notice-banner success" role="status">
            {notice}
          </div>
        )}

        {!hasCompletedReset && (
          <form onSubmit={handleSubmit}>
            <div className="form-group">
              <label>New Password</label>
              <input
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                autoComplete="new-password"
              />
            </div>

            <div className="form-group">
              <label>Confirm Password</label>
              <input
                type="password"
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                autoComplete="new-password"
              />
            </div>

            <button type="submit" className="primary-btn" disabled={loading}>
              {loading ? "Resetting..." : "Reset Password"}
            </button>
          </form>
        )}

        <div className="auth-link-row single">
          {hasCompletedReset ? (
            <Link className="primary-btn auth-link-button" to="/login">
              Go to Login
            </Link>
          ) : error ? (
            <Link className="auth-link-btn auth-link-anchor" to="/login?mode=forgot">
              Request another reset link
            </Link>
          ) : (
            <Link className="auth-link-btn auth-link-anchor" to="/login">
              Back to log in
            </Link>
          )}
        </div>
      </div>
    </div>
  );
}
