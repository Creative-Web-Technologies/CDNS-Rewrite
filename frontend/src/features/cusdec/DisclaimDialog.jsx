import { AlertTriangle } from 'lucide-react';
import { Button } from '../../components/ui';
import Modal from '../../components/ui/Modal';

export default function DisclaimDialog({ item, busy, onConfirm, onCancel }) {
  const number = item?.cusDecNumber ?? 'this declaration';

  const footer = (
    <div className="cd-dialog__actions">
      <Button variant="secondary" onClick={onCancel} disabled={busy}>
        Keep it
      </Button>
      <Button
        variant="danger"
        onClick={onConfirm}
        disabled={busy}
        aria-busy={busy}
      >
        {busy ? 'Disclaiming…' : 'Disclaim'}
      </Button>
    </div>
  );

  return (
    <Modal
      open={!!item}
      onClose={onCancel}
      title={`Disclaim ${number}?`}
      size="sm"
      footer={footer}
    >
      <div className="cd-dialog__icon" aria-hidden="true">
        <AlertTriangle size={22} />
      </div>
      <p className="cd-dialog__body">
        You are confirming this CusDec does not belong to your company.
        It moves to Disclaimed and can't be moved back.
      </p>
    </Modal>
  );
}
