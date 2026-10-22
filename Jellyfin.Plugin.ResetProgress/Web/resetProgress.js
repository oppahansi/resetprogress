// Reset Progress: adds a "reset progress" button next to the played check mark on Continue Watching cards.
(function () {
    'use strict';

    if (window.ResetProgressLoaded) {
        return;
    }
    window.ResetProgressLoaded = true;

    const BUTTON_CLASS = 'resetProgressButton';

    // Continue Watching and Next Up both monitor video playback, but only Continue Watching cards carry a
    // resume position: the web client asks Next Up to leave resumable items out.
    function isContinueWatchingCard(card) {
        const container = card.closest('.itemsContainer');
        if (!container || !container.closest('.homeSectionsContainer')) {
            return false;
        }

        const monitor = (container.getAttribute('data-monitor') || '').split(',');
        return monitor.includes('videoplayback')
            && (card.hasAttribute('data-positionticks') || !!card.querySelector('.itemProgressBar'));
    }

    function createButton() {
        const button = document.createElement('button');
        button.type = 'button';
        button.className = 'cardOverlayButton cardOverlayButton-hover paper-icon-button-light ' + BUTTON_CLASS;
        button.title = 'Reset progress';
        button.setAttribute('aria-label', 'Reset progress');
        button.innerHTML = '<span class="material-icons cardOverlayButtonIcon cardOverlayButtonIcon-hover replay" aria-hidden="true"></span>';
        button.addEventListener('click', onClick);
        return button;
    }

    function addButton(card) {
        const buttons = card.querySelector('.cardOverlayButton-br');
        if (!buttons || buttons.querySelector('.' + BUTTON_CLASS)) {
            return;
        }

        const played = buttons.querySelector('button[is="emby-playstatebutton"]');
        if (played) {
            played.after(createButton());
        } else {
            buttons.prepend(createButton());
        }
    }

    async function onClick(event) {
        // The card overlay itself is a link to the item, don't let the click reach it.
        event.preventDefault();
        event.stopPropagation();

        const button = event.currentTarget;
        const card = button.closest('.card');
        const itemId = card && card.getAttribute('data-id');
        const apiClient = window.ApiClient;
        if (!itemId || !apiClient || button.disabled) {
            return;
        }

        button.disabled = true;
        try {
            await apiClient.ajax({ type: 'POST', url: apiClient.getUrl('ResetProgress/Items/' + itemId) });
        } catch (err) {
            console.error('[ResetProgress] Resetting progress failed', err);
            button.disabled = false;
            return;
        }

        // The server also sends a UserDataChanged event that refreshes the row; hide the card right away.
        const container = card.closest('.itemsContainer');
        card.style.transition = 'opacity .2s';
        card.style.opacity = '0';
        setTimeout(function () {
            if (container && typeof container.notifyRefreshNeeded === 'function') {
                container.notifyRefreshNeeded(true);
            } else {
                card.remove();
            }
        }, 200);
    }

    let scheduled = false;

    function scan() {
        scheduled = false;
        document.querySelectorAll('.homeSectionsContainer .card[data-id]').forEach(function (card) {
            if (isContinueWatchingCard(card)) {
                addButton(card);
            }
        });
    }

    function scheduleScan() {
        if (!scheduled) {
            scheduled = true;
            requestAnimationFrame(scan);
        }
    }

    new MutationObserver(scheduleScan).observe(document.body, { childList: true, subtree: true });
    scheduleScan();
})();
