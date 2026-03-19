import { useEffect } from "react";
import { NavLink, Outlet, useNavigate } from "react-router-dom";
import { useAuthStore } from "../store/authStore";
import ThemeToggle from "../components/ThemeToggle";
import {
  hasSessionExpired,
  recordSessionActivity,
  setSessionExpiredNotice
} from "../utils/sessionTimeout";

const navigationItems = [
  { to: "/dashboard", label: "Dashboard", hint: "Your money snapshot" },
  { to: "/transactions", label: "Transactions", hint: "Track every move" },
  { to: "/categories", label: "Categories", hint: "Keep spending tidy" },
  { to: "/budgets", label: "Budgets", hint: "Stay on target" },
  { to: "/goals", label: "Goals", hint: "Save with purpose" },
  { to: "/reports", label: "Reports", hint: "See the bigger picture" },
  { to: "/recurring", label: "Recurring", hint: "Bills and repeats" },
  { to: "/accounts", label: "Accounts", hint: "Banks, cards, funds" }
];

export default function AppLayout() {
  const navigate = useNavigate();
  const { user, isReturningUser, logout } = useAuthStore();

  const handleLogout = () => {
    logout();
    navigate("/login");
  };

  useEffect(() => {
    if (!user) return;

    const markActivity = () => {
      recordSessionActivity();
    };

    const handleSessionExpiry = () => {
      if (!hasSessionExpired()) return;

      setSessionExpiredNotice();
      logout();
      navigate("/login", { replace: true });
    };

    const trackedEvents: Array<keyof WindowEventMap> = [
      "click",
      "keydown",
      "scroll",
      "touchstart",
      "mousemove"
    ];

    let lastMouseMoveAt = 0;
    const handleMouseMove = () => {
      const now = Date.now();
      if (now - lastMouseMoveAt < 15000) return;

      lastMouseMoveAt = now;
      markActivity();
    };

    const eventHandlers = trackedEvents.map((eventName) => {
      const handler = eventName === "mousemove" ? handleMouseMove : markActivity;
      window.addEventListener(eventName, handler, { passive: true });
      return { eventName, handler };
    });

    const visibilityHandler = () => {
      if (document.visibilityState === "visible") {
        if (hasSessionExpired()) {
          handleSessionExpiry();
          return;
        }

        markActivity();
      }
    };

    document.addEventListener("visibilitychange", visibilityHandler);
    recordSessionActivity();

    const intervalId = window.setInterval(handleSessionExpiry, 60000);

    return () => {
      eventHandlers.forEach(({ eventName, handler }) => {
        window.removeEventListener(eventName, handler);
      });
      document.removeEventListener("visibilitychange", visibilityHandler);
      window.clearInterval(intervalId);
    };
  }, [user, logout, navigate]);

  return (
    <div className="layout">
      <aside className="sidebar">
        <div className="sidebar-brand">
          <div className="sidebar-brand-mark">PF</div>
          <div>
            <h2>Finance Tracker</h2>
            <div className="sidebar-subtitle">Calm control over daily money decisions</div>
          </div>
        </div>

        <div className="sidebar-section-label">Workspace</div>
        <nav>
          {navigationItems.map((item) => (
            <NavLink key={item.to} to={item.to} className="sidebar-link">
              <span>{item.label}</span>
              <small>{item.hint}</small>
            </NavLink>
          ))}
        </nav>

        <div className="sidebar-panel">
          <div className="sidebar-panel-label">Focus</div>
          <strong>Track today, plan this month.</strong>
          <div className="sidebar-panel-text">
            Keep budgets, goals, bills, and spending in one steady view.
          </div>
        </div>
      </aside>

      <main className="main">
        <div className="topbar">
          <div className="topbar-summary">
            <div className="topbar-kicker">Personal workspace</div>
            <div className="topbar-heading">
              {isReturningUser ? "Welcome back" : "Welcome"}, {user?.displayName}
            </div>
            <div className="topbar-meta">
              Your full finance view is ready for planning, tracking, and review.
            </div>
          </div>
          <div className="topbar-actions">
            <div className="profile-chip">
              <span className="profile-chip-name">{user?.displayName}</span>
              <span className="badge">{user?.role}</span>
            </div>
            <ThemeToggle />
            <button className="secondary-btn logout-btn" onClick={handleLogout}>
              Logout
            </button>
          </div>
        </div>

        <div className="content-shell">
          <Outlet />
        </div>
      </main>
    </div>
  );
}
