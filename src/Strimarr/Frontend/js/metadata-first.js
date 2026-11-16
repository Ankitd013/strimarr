(function () {
    'use strict';
    let current = null;
    const states = new WeakMap();
    const norm = id => String(id || '').replace(/-/g, '').toLowerCase();
    function buttons(page, disabled) {
        page.querySelectorAll('.btnPlay, .btnResume').forEach(b => {
            if (disabled && !b.disabled) { b.dataset.clawizPending = '1'; b.disabled = true; }
            else if (!disabled && b.dataset.clawizPending) { b.disabled = false; delete b.dataset.clawizPending; }
        });
    }
    function clear(state, dispose = false) {
        if (dispose) state.control?.remove();
        clearTimeout(state.timer);
        state.status?.remove();
        state.status = null;
        buttons(state.page, false);
    }
    function pending(state) {
        if (!state.status) {
            const status = document.createElement('div');
            status.className = 'clawizStreamStatus';
            status.setAttribute('role', 'status');
            status.style.cssText = 'margin:.6em 0;opacity:.8';
            status.textContent = 'Finding playable versions…';
            const target = state.page.querySelector('.trackSelections');
            if (target) target.insertAdjacentElement('afterend', status);
            state.status = status;
        }
        buttons(state.page, true);
    }
    function hiddenControl(state, item) {
        state.includeHidden = item.ProviderIds?.ClawizHiddenVersions === 'true';
        if (!state.control) {
            const wrap = document.createElement('div');
            wrap.style.cssText = 'margin:.8em 0';
            const button = document.createElement('button');
            button.type = 'button';
            button.className = 'emby-button raised';
            button.style.minHeight = '44px';
            const note = document.createElement('div');
            note.style.cssText = 'margin:.4em 0;opacity:.8';
            note.textContent = 'Includes other and unidentified audio languages for this title. Links are still checked.';
            wrap.append(button, note);
            const target = state.page.querySelector('.trackSelections');
            if (!target) return;
            target.insertAdjacentElement('afterend', wrap);
            state.control = wrap; state.toggle = button; state.languageNote = note;
            button.addEventListener('click', async event => {
                event.preventDefault();
                event.stopPropagation();
                const api = window.ApiClient;
                const include = !state.includeHidden;
                button.disabled = true; button.setAttribute('aria-busy', 'true');
                button.textContent = 'Updating versions…';
                try {
                    await api.ajax({ type: 'POST', dataType: 'json',
                        url: api.getUrl('clawiz/versions/' + state.id, { includeHidden: include }) });
                    if (current !== state) return;
                    state.attempts = 0; state.refreshed = false;
                    clearTimeout(state.timer); pending(state);
                    state.status.textContent = include ? 'Checking hidden versions…' : 'Checking preferred languages…';
                    await poll(state);
                } catch (error) {
                    note.textContent = 'Could not update versions. Try again.';
                    button.textContent = state.includeHidden ? 'Use preferred languages only' : 'Show hidden versions';
                } finally {
                    button.disabled = false; button.removeAttribute('aria-busy');
                }
            });
        }
        const filtered = item.ProviderIds?.ClawizLanguageFilterActive !== 'false';
        state.toggle.hidden = !filtered;
        state.languageNote.textContent = filtered
            ? 'Includes other and unidentified audio languages for this title. Links are still checked.'
            : 'All audio languages are available. Original-audio releases are preferred when identified.';
        state.toggle.textContent = state.includeHidden ? 'Use preferred languages only' : 'Show hidden versions';
        state.toggle.setAttribute('aria-pressed', String(state.includeHidden));
    }
    async function poll(state) {
        if (current !== state || state.page.classList.contains('hide')) return;
        const api = window.ApiClient;
        if (!api || !state.page.querySelector('h1')?.textContent.trim()) {
            state.timer = setTimeout(() => poll(state), 300);
            return;
        }
        try {
            const item = await api.getItem(api.getCurrentUserId(), state.id);
            if (current !== state) return;
            const remote = item.ProviderIds?.Stremio || String(item.Path || '').startsWith('gelato://');
            if (!remote) { clear(state); return; }
            hiddenControl(state, item);
            const ready = (item.MediaSources || []).some(s => s.Name && s.Type !== 'Placeholder');
            if (ready && item.ProviderIds?.ClawizStreamStatus !== 'pending') {
                clear(state);
                if (!state.refreshed) {
                    state.refreshed = true;
                    // Use the page's existing reload handler so its playback-source state,
                    // version selector and audio/subtitle controls are refreshed together.
                    // The rendered poster, title and synopsis remain in the DOM.
                    state.page.dispatchEvent(new CustomEvent('viewshow', {
                        bubbles: true, detail: { isRestored: false }
                    }));
                }
                return;
            }
            pending(state);
            const outcome = item.ProviderIds?.ClawizStreamStatus;
            if (outcome === 'empty' || outcome === 'error') {
                state.status.textContent = outcome === 'empty'
                    ? 'No playable version was found from your sources in the allowed languages. Reopen after a minute to retry.'
                    : 'Source lookup failed. Reopen after a minute to retry.';
                if (!state.refreshed) {
                    state.refreshed = true;
                    state.page.dispatchEvent(new CustomEvent('viewshow', {
                        bubbles: true, detail: { isRestored: false }
                    }));
                }
                return;
            }
        } catch (error) {
            console.warn('[Clawiz] Stream readiness check failed', error);
        }
        if (++state.attempts < 30) state.timer = setTimeout(() => poll(state), 2000);
        else {
            if (state.status) state.status.textContent = 'Source lookup is taking too long. Reopen this title to check again.';
        }
    }
    document.addEventListener('viewshow', event => {
        const page = event.target;
        if (!page?.querySelector?.('.selectSource')) return;
        const id = new URLSearchParams(location.hash.split('?')[1] || location.search).get('id');
        if (!id) return;
        const old = states.get(page);
        if (old && norm(old.id) === norm(id) && old === current) return;
        if (current) clear(current, true);
        const state = { page, id, attempts: 0, refreshed: false };
        states.set(page, state); current = state;
        state.timer = setTimeout(() => poll(state), 700);
    });
    document.addEventListener('viewbeforehide', event => {
        if (current?.page === event.target) { clear(current, true); current = null; }
    });
})();
