import { useEffect, useRef, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import ThemeToggle from "../components/ThemeToggle";
import { authService } from "../services/authService";

export default function VerifyEmailPage() {
  const [searchParams] = useSearchParams();
  const [loading, setLoading] = useState(true);
  const [message, setMessage] = useState("Verifying your email...");
  const [error, setError] = useState("");
  const hasAttemptedVerification = useRef(false);

  useEffect(() => {
    if (hasAttemptedVerification.current) {
      return;
    }

    hasAttemptedVerification.current = true;
    const token = searchParams.get("token");

    const verify = async () => {
      if (!token) {
        setError("This verification link is missing a token.");
        setLoading(false);
        return;
      }

      try {
        const result = await authService.verifyEmail({ token });
        setMessage(result.message);
      } catch (err: any) {
        setError(err?.response?.data?.message || "Failed to verify this email link.");
      } finally {
        setLoading(false);
      }
    };

    verify();
  }, [searchParams]);

  return (
    <div className="auth-page">
      <div className="auth-card">
        <div className="auth-header">
          <h1>Email Verification</h1>
          <ThemeToggle />
        </div>

        {error ? (
          <div className="error-text" role="alert">
            {error}
          </div>
        ) : (
          <div className="notice-banner success" role="status">
            {message}
          </div>
        )}

        <p className="auth-note">
          {loading ? "Please wait while we confirm your email." : "You can head back to the login screen now."}
        </p>

        <Link className="primary-btn auth-link-button" to="/login">
          Go to Login
        </Link>
      </div>
    </div>
  );
}
