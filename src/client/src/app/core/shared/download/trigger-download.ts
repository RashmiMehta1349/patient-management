/**
 * Module 8 (Data Export) — shared client-side "trigger blob download" helper (plan §10 task 14),
 * reused by both patient and visit export flows rather than duplicated per component. Creates a
 * transient object URL, clicks a detached anchor with a `download` attribute to force a save-as
 * file (distinct from Module 5's "print" framing, which opens the PDF in a new tab instead), then
 * revokes the URL shortly after so no export content lingers client-side beyond the download.
 */
export function triggerDownload(blob: Blob, fileName: string): void {
  const url = window.URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName;
  link.click();
  setTimeout(() => window.URL.revokeObjectURL(url), 30000);
}
