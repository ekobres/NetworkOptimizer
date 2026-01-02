// Demo Mode Masking - Masks sensitive strings for screen sharing/recording
(function () {
    'use strict';

    let mappings = [];
    let isEnabled = false;
    let observer = null;
    let formFieldInterval = null;

    // Load mappings from backend
    async function loadMappings() {
        try {
            const response = await fetch('/api/demo-mappings', { credentials: 'include' });
            if (response.ok) {
                const data = await response.json();
                if (data && data.mappings && data.mappings.length > 0) {
                    mappings = data.mappings;
                    isEnabled = true;
                    return true;
                }
            }
        } catch (e) {
            // Silently fail - demo mode just won't be active
        }
        return false;
    }

    // Apply masking to a string
    function maskString(text) {
        if (!isEnabled || !text) return text;
        let result = text;
        for (const mapping of mappings) {
            // Case-insensitive replacement
            const regex = new RegExp(escapeRegExp(mapping.from), 'gi');
            result = result.replace(regex, mapping.to);
        }
        return result;
    }

    // Escape special regex characters
    function escapeRegExp(string) {
        return string.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    }

    // Check if element should be skipped
    function shouldSkipElement(element) {
        if (!element) return true;
        // Skip script and style elements
        if (element.tagName === 'SCRIPT' || element.tagName === 'STYLE') return true;
        // Skip elements with data-no-mask attribute
        if (element.hasAttribute && element.hasAttribute('data-no-mask')) return true;
        return false;
    }

    // Mask text content of an element
    function maskTextNode(node) {
        if (node.nodeType === Node.TEXT_NODE && node.textContent) {
            const masked = maskString(node.textContent);
            if (masked !== node.textContent) {
                node.textContent = masked;
            }
        }
    }

    // Check if value needs masking
    function needsMasking(value) {
        if (!value) return false;
        for (const mapping of mappings) {
            const regex = new RegExp(escapeRegExp(mapping.from), 'gi');
            if (regex.test(value)) return true;
        }
        return false;
    }

    // Mask form field values
    function maskFormField(element) {
        if (element.tagName === 'INPUT' || element.tagName === 'TEXTAREA') {
            const currentValue = element.value;

            // Skip if no value or already focused (user is editing)
            if (!currentValue || document.activeElement === element) return;

            // Check if this value needs masking
            if (needsMasking(currentValue)) {
                // Store original for restoration
                element.dataset.originalValue = currentValue;
                element.value = maskString(currentValue);

                // Add focus/blur listeners if not already added
                if (!element.dataset.maskListenersAdded) {
                    element.addEventListener('focus', function() {
                        if (this.dataset.originalValue) {
                            this.value = this.dataset.originalValue;
                        }
                    });
                    element.addEventListener('blur', function() {
                        if (this.value && needsMasking(this.value)) {
                            this.dataset.originalValue = this.value;
                            this.value = maskString(this.value);
                        }
                    });
                    element.dataset.maskListenersAdded = 'true';
                }
            }
        } else if (element.tagName === 'SELECT') {
            // Mask select option text
            for (const option of element.options) {
                const masked = maskString(option.textContent);
                if (masked !== option.textContent) {
                    option.textContent = masked;
                }
            }
        }
    }

    // Mask all form fields on the page
    function maskAllFormFields() {
        if (!isEnabled) return;
        const formFields = document.querySelectorAll('input, textarea, select');
        for (const field of formFields) {
            maskFormField(field);
        }
    }

    // Mask all content in an element tree
    function maskElement(element) {
        if (!isEnabled) return;
        if (shouldSkipElement(element)) return;

        // Walk all text nodes
        const walker = document.createTreeWalker(
            element,
            NodeFilter.SHOW_TEXT,
            null,
            false
        );

        const textNodes = [];
        while (walker.nextNode()) {
            textNodes.push(walker.currentNode);
        }

        for (const node of textNodes) {
            if (!shouldSkipElement(node.parentElement)) {
                maskTextNode(node);
            }
        }

        // Mask form fields
        const formFields = element.querySelectorAll('input, textarea, select');
        for (const field of formFields) {
            maskFormField(field);
        }
    }

    // Set up MutationObserver to handle dynamic content
    function setupObserver() {
        if (observer) return;

        observer = new MutationObserver((mutations) => {
            for (const mutation of mutations) {
                // Handle added nodes
                if (mutation.type === 'childList') {
                    for (const node of mutation.addedNodes) {
                        if (node.nodeType === Node.ELEMENT_NODE) {
                            maskElement(node);
                        } else if (node.nodeType === Node.TEXT_NODE) {
                            maskTextNode(node);
                        }
                    }
                }
                // Handle text content changes
                else if (mutation.type === 'characterData') {
                    maskTextNode(mutation.target);
                }
            }
        });

        observer.observe(document.body, {
            childList: true,
            subtree: true,
            characterData: true
        });
    }

    // Start periodic form field checking (for Blazor-set values)
    function startFormFieldPolling() {
        if (formFieldInterval) return;
        // Check form fields every 500ms for new values
        formFieldInterval = setInterval(maskAllFormFields, 500);
    }

    // Initialize demo masking
    async function init() {
        const enabled = await loadMappings();
        if (enabled) {
            // Initial masking of existing content
            maskElement(document.body);
            // Watch for dynamic changes
            setupObserver();
            // Poll for form field value changes (Blazor sets these programmatically)
            startFormFieldPolling();
            console.log('Demo mode active');
        }
    }

    // Start when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    // Also re-run after Blazor enhanced navigation
    if (typeof Blazor !== 'undefined') {
        Blazor.addEventListener('enhancedload', function() {
            if (isEnabled) {
                setTimeout(() => maskElement(document.body), 100);
            }
        });
    }

    // Expose for manual re-masking if needed
    window.DemoMask = {
        refresh: () => {
            maskElement(document.body);
            maskAllFormFields();
        },
        isEnabled: () => isEnabled
    };
})();
