export function addCss(fileName) {

    var head = document.head;
    var link = document.createElement("link");

    link.type = "text/css";
    link.rel = "stylesheet";
    link.href = fileName;
    link.className = "theme";

    head.appendChild(link);
}

export function removeThemes() {

    var elements = document.getElementsByClassName("theme");

    if (elements.length == 0) return;

    for (var i = elements.length - 1, l = elements.length; i >= 0; i--) {
        elements[i].remove();
    }
}

export function moveOpenGraphMetaTagsToTop() {
    document.querySelectorAll('head > meta[property^="og:"]')
        .forEach(meta => {
            document.head.prepend(meta);
        });
}

export function scrollToElement(elementId) {
    if (!elementId) {
        return;
    }

    const target = document.getElementById(elementId);
    if (!target) {
        return;
    }

    const scrollContainer = target.closest('.main-content');
    const fallbackScroll = () => target.scrollIntoView({ behavior: 'smooth', block: 'start' });

    if (!scrollContainer) {
        fallbackScroll();
    }
    else {
        const containerRect = scrollContainer.getBoundingClientRect();
        const targetRect = target.getBoundingClientRect();
        const currentScrollTop = scrollContainer.scrollTop;
        const offset = 16; // keep content slightly below the header

        const destination = currentScrollTop + (targetRect.top - containerRect.top) - offset;
        scrollContainer.scrollTo({ top: destination, behavior: 'smooth' });
    }
}