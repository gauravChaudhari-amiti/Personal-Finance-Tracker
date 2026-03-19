import { useEffect, useRef, useState } from "react";
import { authService } from "../services/authService";

type Props = {
  onCredential: (credential: string) => void | Promise<void>;
};

const googleClientIdFromEnv = import.meta.env.VITE_GOOGLE_CLIENT_ID?.trim() ?? "";

export default function GoogleSignInButton({ onCredential }: Props) {
  const buttonContainerRef = useRef<HTMLDivElement | null>(null);
  const [googleClientId, setGoogleClientId] = useState(googleClientIdFromEnv);

  useEffect(() => {
    if (googleClientIdFromEnv) {
      setGoogleClientId(googleClientIdFromEnv);
      return;
    }

    let cancelled = false;

    const loadGoogleConfig = async () => {
      try {
        const config = await authService.getGoogleConfig();
        if (!cancelled) {
          setGoogleClientId(config.clientId?.trim() ?? "");
        }
      } catch {
        if (!cancelled) {
          setGoogleClientId("");
        }
      }
    };

    void loadGoogleConfig();

    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    if (!googleClientId || !buttonContainerRef.current) {
      return;
    }

    let disposed = false;
    let pollHandle: number | null = null;

    const renderButton = () => {
      if (disposed || !window.google || !buttonContainerRef.current) {
        return false;
      }

      window.google.accounts.id.initialize({
        client_id: googleClientId,
        callback: ({ credential }) => {
          if (credential) {
            void onCredential(credential);
          }
        }
      });

      buttonContainerRef.current.innerHTML = "";
      window.google.accounts.id.renderButton(buttonContainerRef.current, {
        theme: "outline",
        size: "large",
        text: "continue_with",
        shape: "pill",
        width: 392
      });

      return true;
    };

    if (!renderButton()) {
      pollHandle = window.setInterval(() => {
        if (renderButton() && pollHandle !== null) {
          window.clearInterval(pollHandle);
          pollHandle = null;
        }
      }, 300);
    }

    return () => {
      disposed = true;
      if (pollHandle !== null) {
        window.clearInterval(pollHandle);
      }
    };
  }, [googleClientId, onCredential]);

  if (!googleClientId) {
    return null;
  }

  return (
    <>
      <div className="auth-divider">
        <span>or continue with</span>
      </div>
      <div ref={buttonContainerRef} className="google-signin-button" />
    </>
  );
}
