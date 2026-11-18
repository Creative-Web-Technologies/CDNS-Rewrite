import { useParams } from 'react-router-dom';

// Returns the :id route param as a positive integer, or null when it is
// missing, non-numeric, zero, or negative. Pages use this to redirect early
// instead of calling the API with a non-numeric segment.
export function useNumericIdParam() {
  const { id } = useParams();
  const n = parseInt(id, 10);
  return Number.isInteger(n) && n > 0 ? n : null;
}
