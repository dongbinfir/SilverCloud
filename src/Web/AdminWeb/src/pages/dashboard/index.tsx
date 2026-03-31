import { Result, Button } from 'antd';
import { useNavigate } from 'react-router-dom';
import { useAuthStore } from '@store/authStore';

const DashboardPage = () => {
  const navigate = useNavigate();
  const logout = useAuthStore((state) => state.logout);

  return (
    <Result
      status="success"
      title="Welcome to Silver Cloud"
      subTitle="You have successfully logged in."
      extra={[
        <Button
          key="logout"
          onClick={() => {
            logout();
            navigate('/login', { replace: true });
          }}
        >
          Logout
        </Button>,
      ]}
    />
  );
};

export default DashboardPage;
