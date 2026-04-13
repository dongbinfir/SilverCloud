import { Card, Col, Result, Row, Statistic } from 'antd';

const DashboardPage = () => {
  return (
    <Row gutter={[16, 16]}>
      <Col xs={24} xl={24}>
        <Card>
          <Result
            status="success"
            title="欢迎进入 Silver Cloud Admin"
            subTitle="当前后台导航结构已就绪，可通过右侧菜单打开页面，并自动在顶部生成标签页。"
          />
        </Card>
      </Col>
      <Col xs={24} md={8}>
        <Card>
          <Statistic title="已打开模块" value={3} suffix="个" />
        </Card>
      </Col>
      <Col xs={24} md={8}>
        <Card>
          <Statistic title="当前导航模式" value="Menu + Tabs" />
        </Card>
      </Col>
      <Col xs={24} md={8}>
        <Card>
          <Statistic title="布局状态" value="Ready" />
        </Card>
      </Col>
    </Row>
  );
};

export default DashboardPage;
