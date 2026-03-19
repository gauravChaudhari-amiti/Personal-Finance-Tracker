import { Navigate } from "react-router-dom";
import { useAuthStore } from "../store/authStore";

type Props = {
  children: React.ReactNode;
};

export default function ProtectedRoute({ children }: Props) {
  const { user, isAuthResolved } = useAuthStore((state) => ({
    user: state.user,
    isAuthResolved: state.isAuthResolved
  }));

  if (!isAuthResolved) {
    return <div>Loading session...</div>;
  }

  if (!user) {
    return <Navigate to="/login" replace />;
  }

  return <>{children}</>;
}
