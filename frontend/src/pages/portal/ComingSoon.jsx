import { Construction } from 'lucide-react';
import { Card, EmptyState, PageTransition } from '../../components/ui';

export default function ComingSoon({ title }) {
  return (
    <PageTransition>
      <Card>
        <EmptyState
          icon={Construction}
          title={title}
          message="This section is under construction and will be available soon."
        />
      </Card>
    </PageTransition>
  );
}
