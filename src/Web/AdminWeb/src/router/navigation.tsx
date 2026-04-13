import {
  HomeOutlined,
  SafetyCertificateOutlined,
  UserOutlined,
} from '@ant-design/icons';
import type { ItemType } from 'antd/es/menu/interface';
import type { ReactNode } from 'react';

export interface NavigationItem {
  key: string;
  label: string;
  icon: ReactNode;
  closable?: boolean;
}

export const navigationItems: NavigationItem[] = [
  {
    key: '/dashboard',
    label: '工作台',
    icon: <HomeOutlined />,
    closable: false,
  },
  {
    key: '/profiles',
    label: '用户资料',
    icon: <UserOutlined />,
  },
  {
    key: '/authorizations',
    label: '权限管理',
    icon: <SafetyCertificateOutlined />,
  },
];

export const menuItems: ItemType[] = navigationItems.map((item) => ({
  key: item.key,
  icon: item.icon,
  label: item.label,
}));

export const navigationMap = navigationItems.reduce<Record<string, NavigationItem>>(
  (result, item) => {
    result[item.key] = item;
    return result;
  },
  {},
);

export const defaultNavigationKey = '/dashboard';