(function contentRequestsTabBridge() {
    'use strict';

    if (window.contentRequestsTabBridge) return;

    const bridge = window.contentRequestsTabBridge = {
        running: false,
        scheduled: false,

        isHome() {
            const hash = window.location.hash;
            return hash === '' || hash === '#/home' || hash === '#/home.html'
                || hash.includes('#/home?') || hash.includes('#/home.html?');
        },

        request(path) {
            return ApiClient.fetch({
                url: ApiClient.getUrl(path),
                type: 'GET',
                dataType: 'json',
                headers: { accept: 'application/json' }
            });
        },

        normalizePaneOrder(favorites) {
            const panes = Array.from(document.querySelectorAll('[id^="customTab_"]'))
                .sort((left, right) => Number(left.id.slice(10)) - Number(right.id.slice(10)));
            let anchor = favorites;
            panes.forEach(pane => {
                if (anchor.nextElementSibling !== pane) anchor.insertAdjacentElement('afterend', pane);
                anchor = pane;
            });
        },

        ensureAdminIcon() {
            const links = document.querySelectorAll(
                'a[href*="configurationpage?name=content-requests"]'
            );

            links.forEach(link => {
                const currentIcon = link.querySelector('svg');
                const container = currentIcon?.parentElement;
                if (!container || container.dataset.contentRequestsIcon === 'true') return;

                const icon = document.createElement('span');
                icon.className = 'material-icons';
                icon.setAttribute('aria-hidden', 'true');
                icon.style.fontSize = '1.5rem';
                icon.textContent = 'add_home';
                container.replaceChildren(icon);
                container.dataset.contentRequestsIcon = 'true';
            });
        },

        ensureSidebarLink(tabName, enabled, index) {
            let link = document.getElementById('contentRequestsSidebarLink');
            if (!link) {
                const homeLink = document.querySelector('.mainDrawer a[href="#/home"], .mainDrawer a[href="#/home.html"], a.navMenuOption[href="#/home"]');
                if (!homeLink) return;
                link = homeLink.cloneNode(true);
                link.id = 'contentRequestsSidebarLink';
                link.href = '#/home';
                link.removeAttribute('data-itemid');
                link.addEventListener('click', event => {
                    event.preventDefault();
                    window.location.hash = '#/home';
                    window.setTimeout(() => {
                        bridge.schedule();
                        const button = document.getElementById('customTabButton_' + index);
                        if (button) button.click();
                    }, 500);
                });
                homeLink.insertAdjacentElement('afterend', link);
            }

            link.style.display = enabled ? '' : 'none';
            const label = link.querySelector('.navMenuOptionText, .emby-button-foreground');
            if (label) label.textContent = tabName;
            const existingIcon = link.querySelector('svg, .material-icons, .material-symbols-rounded');
            const iconContainer = existingIcon?.parentElement;
            if (iconContainer && iconContainer.dataset.contentRequestsIcon !== 'true') {
                const icon = document.createElement('span');
                icon.className = 'material-icons';
                icon.setAttribute('aria-hidden', 'true');
                icon.style.cssText = 'display:inline-flex;align-items:center;justify-content:center;width:1.5rem;flex:0 0 1.5rem;margin-right:1.2rem;font-size:1.5rem';
                icon.textContent = 'add_home';
                if (iconContainer === link || (label && iconContainer.contains(label))) {
                    link.querySelectorAll('svg, .material-icons, .material-symbols-rounded')
                        .forEach(candidate => candidate.remove());
                    label ? link.insertBefore(icon, label) : link.prepend(icon);
                    link.dataset.contentRequestsIcon = 'true';
                } else {
                    iconContainer.replaceChildren(icon);
                    iconContainer.dataset.contentRequestsIcon = 'true';
                }
            }
        },

        async repair() {
            this.ensureAdminIcon();
            if (this.running || !this.isHome() || typeof ApiClient === 'undefined') return;
            this.running = true;

            try {
                const configs = await this.request('CustomTabs/Config');
                const index = configs.findIndex(config =>
                    String(config.ContentHtml || '').includes('ContentRequests/Form'));
                if (index < 0) return;

                const button = document.getElementById(`customTabButton_${index}`);
                const favorites = document.getElementById('favoritesTab');
                if (!button || !favorites) return;

                const settings = await this.request('ContentRequests/DisplaySettings');
                const enabled = settings.TabEnabled ?? settings.tabEnabled ?? true;
                const tabName = settings.TabName ?? settings.tabName ?? 'Requests';
                button.style.display = enabled ? '' : 'none';
                const label = button.querySelector('.emby-button-foreground');
                if (label) label.textContent = tabName;
                this.ensureSidebarLink(tabName, enabled, index);

                let pane = document.getElementById(`customTab_${index}`);
                if (!pane) {
                    pane = document.createElement('div');
                    pane.id = `customTab_${index}`;
                    pane.className = 'tabContent pageTabContent';
                    pane.dataset.index = String(index + 2);
                    favorites.insertAdjacentElement('afterend', pane);
                }

                let iframe = pane.querySelector('iframe');
                if (!iframe || !String(iframe.getAttribute('src') || '').includes('ContentRequests/Form')) {
                    iframe = document.createElement('iframe');
                    iframe.title = tabName;
                    iframe.src = ApiClient.getUrl('ContentRequests/Form');
                    iframe.style.cssText = 'display:block;width:100%;height:calc(100vh - 7.5rem);min-height:32rem;border:0;background:transparent';
                    pane.replaceChildren(iframe);
                    console.info('Content Requests: repaired its CustomTabs content pane.');
                }
                this.normalizePaneOrder(favorites);
                pane.style.display = enabled ? '' : 'none';
            } catch (error) {
                console.debug('Content Requests: homepage-tab bridge is waiting for CustomTabs.', error);
            } finally {
                this.running = false;
            }
        },

        schedule() {
            if (this.scheduled) return;
            this.scheduled = true;
            window.setTimeout(() => {
                this.scheduled = false;
                this.repair();
            }, 100);
        }
    };

    new MutationObserver(() => bridge.schedule()).observe(document.documentElement, {
        childList: true,
        subtree: true
    });
    window.addEventListener('hashchange', () => bridge.schedule());
    window.addEventListener('pageshow', () => bridge.schedule());
    bridge.schedule();
}());
