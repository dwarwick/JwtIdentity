(function () {
    function getScrollableAncestor(el) {
        const isScrollable = (node) => {
            const cs = getComputedStyle(node);
            const oy = cs.overflowY;
            return /(auto|scroll|overlay)/.test(oy) && node.scrollHeight > node.clientHeight;
        };

        for (let p = el.parentElement; p; p = p.parentElement) {
            if (isScrollable(p)) return p;
        }
        // Fallback to the document scroller
        return document.scrollingElement || document.documentElement;
    }

    function offsetTopWithin(el, ancestor) {
        // Accumulate offsetTop until the specified ancestor
        let top = 0, n = el;
        while (n && n !== ancestor) {
            top += n.offsetTop;
            n = n.offsetParent;
        }
        return top;
    }

    // idOrEl: string id OR an element
    // options: { behavior?: 'smooth'|'auto', block?: 'start'|'center'|'end', headerOffset?: number }
    window.scrollToElement = function (idOrEl, options) {
        const opts = options || {};
        const behavior = opts.behavior || 'smooth';
        const block = opts.block || 'start';
        const headerOffset = Number(opts.headerOffset || 0);

        const el = (typeof idOrEl === 'string')
            ? document.getElementById(idOrEl)
            : idOrEl;

        if (!el) return false;

        // Delay to next frame so layout is stable (helps right after Blazor re-render)
        requestAnimationFrame(() => {
            const container = getScrollableAncestor(el);

            if (container === document.scrollingElement || container === document.documentElement) {
                // Window scrolling path (supports header offset)
                const rect = el.getBoundingClientRect();
                const currentY = window.pageYOffset || document.documentElement.scrollTop;
                let targetY;

                switch (block) {
                    case 'center':
                        targetY = rect.top + currentY - (window.innerHeight / 2) + (el.offsetHeight / 2) - headerOffset;
                        break;
                    case 'end':
                        targetY = rect.bottom + currentY - window.innerHeight - headerOffset;
                        break;
                    case 'start':
                    default:
                        targetY = rect.top + currentY - headerOffset;
                        break;
                }

                window.scrollTo({ top: Math.max(0, targetY), behavior });
            } else {
                // Nested scroll container path
                const targetTop = offsetTopWithin(el, container);
                let top;
                switch (block) {
                    case 'center':
                        top = targetTop - (container.clientHeight / 2) + (el.offsetHeight / 2) - headerOffset;
                        break;
                    case 'end':
                        top = targetTop - container.clientHeight + el.offsetHeight - headerOffset;
                        break;
                    case 'start':
                    default:
                        top = targetTop - headerOffset;
                        break;
                }
                container.scrollTo({ top: Math.max(0, top), behavior });
            }
        });

        return true;
    };
})();
