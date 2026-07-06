export function openMenu(menuId, anchorId) {
    if (!menuId) {
        return false;
    }

    const menu = document.getElementById(menuId);
    if (!menu || typeof menu.openMenu !== 'function') {
        return false;
    }

    const open = () => {
        if (!menu.isConnected) {
            return;
        }

        const anchor = anchorId ? document.getElementById(anchorId) : null;
        const list = menu.querySelector('fluent-menu-list');
        if (anchor && list) {
            const rect = anchor.getBoundingClientRect();
            list.style.position = 'fixed';
            list.style.margin = '0';
            list.style.top = `${rect.bottom}px`;
            list.style.left = `${rect.left}px`;
        }

        menu.openMenu();
    };

    setTimeout(open, 250);

    return true;
}
