import { Avatar, Button, Layout, Menu, Popover, Space, Tabs, Typography } from 'antd';
import type { TabsProps } from 'antd';
import { Outlet, useLocation, useNavigate } from 'react-router-dom';
import { useEffect, useState } from 'react';
import {
  BellOutlined,
  QuestionCircleOutlined,
  UserOutlined,
} from '@ant-design/icons';
import { useAuthStore } from '@store/authStore';
import {
  defaultNavigationKey,
  menuItems,
  navigationMap,
  navigationItems,
} from '@router/navigation';

const { Header, Content, Sider } = Layout;
const { Text, Title } = Typography;

type TabItem = NonNullable<TabsProps['items']>[number];

function createTabItem(key: string): TabItem {
  const currentItem = navigationMap[key];

  return {
    key: currentItem.key,
    label: currentItem.label,
    closable: currentItem.closable !== false,
  };
}

const AdminLayout = () => {
  const location = useLocation();
  const navigate = useNavigate();
  const logout = useAuthStore((state) => state.logout);
  const accountName = useAuthStore((state) => state.accountName);
  const email = useAuthStore((state) => state.email);
  const phoneNum = useAuthStore((state) => state.phoneNum);

  const [tabs, setTabs] = useState<TabItem[]>([createTabItem(defaultNavigationKey)]);

  const activeKey = navigationMap[location.pathname]
    ? location.pathname
    : defaultNavigationKey;

  useEffect(() => {
    if (!navigationMap[activeKey]) {
      return;
    }

    setTabs((currentTabs) => {
      if (currentTabs.some((tab) => tab.key === activeKey)) {
        return currentTabs;
      }

      return [...currentTabs, createTabItem(activeKey)];
    });
  }, [activeKey]);

  const onMenuChange = (key: string) => {
    navigate(key);
  };

  const onTabChange = (key: string) => {
    navigate(key);
  };

  const onTabEdit: TabsProps['onEdit'] = (targetKey, action) => {
    if (action !== 'remove' || typeof targetKey !== 'string') {
      return;
    }

    setTabs((currentTabs) => {
      const nextTabs = currentTabs.filter((tab) => tab.key !== targetKey);

      if (nextTabs.length === 0) {
        navigate(defaultNavigationKey);
        return [createTabItem(defaultNavigationKey)];
      }

      if (targetKey === activeKey) {
        const targetIndex = currentTabs.findIndex((tab) => tab.key === targetKey);
        const fallbackTab = nextTabs[targetIndex - 1] ?? nextTabs[targetIndex] ?? nextTabs[0];

        if (fallbackTab?.key) {
          navigate(fallbackTab.key);
        }
      }

      return nextTabs;
    });
  };

  return (
    <Layout className="admin-shell">
      <Header className="admin-shell__header">
        <div className="admin-shell__brand">
          <div className="admin-shell__brand-mark">SC</div>
          <div className="admin-shell__brand-meta">
          <Title level={5} className="admin-shell__title">
            Silver Cloud Admin
          </Title>
            <Text type="secondary" className="admin-shell__brand-subtitle">
              Workspace
            </Text>
          </div>
        </div>
        <Space size={6} className="admin-shell__actions">
          <Button size="small" type="text" icon={<QuestionCircleOutlined />} />
          <Button size="small" type="text" icon={<BellOutlined />} />
          <Space size={6} className="admin-shell__user">
            <Avatar size={26} icon={<UserOutlined />} />
            <Text type="secondary" className="admin-shell__user-name">
              {accountName ?? email ?? phoneNum ?? '当前用户'}
            </Text>
            <Popover
              trigger="hover"
              placement="bottomRight"
              content={
                <Button
                  type="primary"
                  danger
                  onClick={() => {
                    logout();
                    navigate('/login', { replace: true });
                  }}
                >
                  退出登录
                </Button>
              }
            >
              <Button className="admin-shell__logout-trigger" size="small" type="text">
                账户操作
              </Button>
            </Popover>
          </Space>
        </Space>
      </Header>
      <Layout className="admin-shell__body">
        <Sider width={240} theme="light" className="admin-shell__sider">
          <div className="admin-shell__menu-header">
            <Text type="secondary">Navigation</Text>
          </div>
          <Menu
            mode="inline"
            items={menuItems}
            selectedKeys={[activeKey]}
            onClick={({ key }) => onMenuChange(key)}
          />
          <div className="admin-shell__menu-footer">
            <Text type="secondary">已配置 {navigationItems.length} 个菜单项</Text>
          </div>
        </Sider>
        <Layout className="admin-shell__workspace">
          <div className="admin-shell__tabs-wrap">
            <Tabs
              hideAdd
              onChange={onTabChange}
              activeKey={activeKey}
              type="editable-card"
              onEdit={onTabEdit}
              items={tabs}
            />
          </div>
          <Content className="admin-shell__content">
            <Outlet />
          </Content>
        </Layout>
      </Layout>
    </Layout>
  );
};

export default AdminLayout;