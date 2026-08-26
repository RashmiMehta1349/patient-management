/** sessionStorage key shared by auth.guard (writer) and LoginComponent (reader/clearer) for the
 * post-login redirect target — kept out of the URL query string so the address bar stays clean. */
export const RETURN_URL_STORAGE_KEY = 'pma_return_url';
