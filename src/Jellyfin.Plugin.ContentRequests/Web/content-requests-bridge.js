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
            const icon = link.querySelector('.material-icons, .material-symbols-rounded');
            if (icon) icon.textContent = 'add_home';
        },

        async repair() {
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

                    const iframe = document.createElement('iframe');
                    iframe.title = tabName;
                    iframe.src = ApiClient.getUrl('ContentRequests/Form');
                    iframe.style.cssText = 'display:block;width:100%;height:calc(100vh - 7.5rem);min-height:32rem;border:0;background:transparent';
                    pane.appendChild(iframe);
                    favorites.insertAdjacentElement('afterend', pane);
                    console.info('Content Requests: repaired missing CustomTabs content pane.');
                }
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
