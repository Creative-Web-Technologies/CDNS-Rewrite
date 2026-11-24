export const cusDecFeatures = {
  detail: true,
  status: true,
  containers: false,
};

// item.IsExited is present in the list DTO: CusDecDtos.cs:73 / CusDecCard.jsx:102.
// Set to true to require exit before showing "Complete CusDec" in the modal.
export const COMPLETE_REQUIRES_EXIT = true;
