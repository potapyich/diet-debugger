import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { ProtectedRoute } from './ProtectedRoute';
import { DayPage } from '../pages/Day/DayPage';
import { ProfilePage } from '../pages/Profile/ProfilePage';
import { WeekPage } from '../pages/Week/WeekPage';
import { AnalyticsPage } from '../pages/Analytics/AnalyticsPage';

const AuthPage = () => <div>Auth</div>;

export function AppRouter() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/auth" element={<AuthPage />} />
        <Route path="/day" element={<ProtectedRoute><DayPage /></ProtectedRoute>} />
        <Route path="/week" element={<ProtectedRoute><WeekPage /></ProtectedRoute>} />
        <Route path="/analytics" element={<ProtectedRoute><AnalyticsPage /></ProtectedRoute>} />
        <Route path="/profile" element={<ProtectedRoute><ProfilePage /></ProtectedRoute>} />
        <Route path="*" element={<ProtectedRoute><DayPage /></ProtectedRoute>} />
      </Routes>
    </BrowserRouter>
  );
}
