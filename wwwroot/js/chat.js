// AI Chat Assistant JavaScript
(function() {
    'use strict';

    // DOM elements
    const chatForm = document.getElementById('chat-form');
    const userInput = document.getElementById('user-input');
    const sendBtn = document.getElementById('send-btn');
    const chatMessages = document.getElementById('chat-messages');
    const recommendationsPanel = document.getElementById('recommendations-panel');

    // Initialize
    document.addEventListener('DOMContentLoaded', function() {
        if (chatForm) {
            chatForm.addEventListener('submit', handleSubmit);
        }
        
        // Clear welcome message on first interaction
        userInput.addEventListener('focus', clearWelcomeMessage, { once: true });
    });

    // Handle form submission
    async function handleSubmit(e) {
        e.preventDefault();
        
        const message = userInput.value.trim();
        if (!message) return;

        // Disable input while processing
        setInputState(false);

        // Add user message to chat
        addMessageToChat('user', message);
        userInput.value = '';

        try {
            // Get anti-forgery token
            const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

            // Send message to backend
            const formData = new FormData();
            formData.append('message', message);
            formData.append('__RequestVerificationToken', token);

            const response = await fetch('?handler=SendMessage', {
                method: 'POST',
                body: formData
            });

            const data = await response.json();

            if (data.success) {
                // Add assistant response
                addMessageToChat('assistant', data.message);

                // Update recommendations panel
                if (data.recommendations && data.recommendations.length > 0) {
                    displayRecommendations(data.recommendations);
                }
            } else {
                addMessageToChat('error', data.error || 'Sorry, something went wrong.');
            }
        } catch (error) {
            console.error('Chat error:', error);
            addMessageToChat('error', 'Sorry, I encountered an error. Please try again.');
        } finally {
            setInputState(true);
            userInput.focus();
        }
    }

    // Add message to chat display
    function addMessageToChat(role, content) {
        const messageDiv = document.createElement('div');
        messageDiv.className = `chat-message chat-message-${role} mb-3`;

        const icon = role === 'user' 
            ? '<i class="bi bi-person-circle"></i>' 
            : role === 'assistant'
            ? '<i class="bi bi-robot"></i>'
            : '<i class="bi bi-exclamation-triangle"></i>';

        messageDiv.innerHTML = `
            <div class="d-flex align-items-start">
                <div class="message-icon me-2">${icon}</div>
                <div class="message-content flex-grow-1">
                    <div class="message-text">${escapeHtml(content)}</div>
                    <div class="message-time">${new Date().toLocaleTimeString()}</div>
                </div>
            </div>
        `;

        chatMessages.appendChild(messageDiv);
        chatMessages.scrollTop = chatMessages.scrollHeight;
    }

    // Display product recommendations
    function displayRecommendations(recommendations) {
        if (!recommendations || recommendations.length === 0) {
            recommendationsPanel.innerHTML = '<p class="text-muted text-center py-4">No recommendations at the moment.</p>';
            return;
        }

        let html = '<div class="recommendations-list">';
        
        recommendations.forEach(rec => {
            html += `
                <div class="recommendation-card mb-3 p-3 border rounded">
                    ${rec.imageUrl ? `<img src="${escapeHtml(rec.imageUrl)}" alt="${escapeHtml(rec.name)}" class="recommendation-image mb-2" />` : ''}
                    <div class="d-flex justify-content-between align-items-start">
                        <div class="flex-grow-1">
                            <h6 class="mb-1">${escapeHtml(rec.name)}</h6>
                            <p class="text-muted mb-1 small">${escapeHtml(rec.sku)}</p>
                            <p class="mb-2"><strong>$${rec.price.toFixed(2)}</strong></p>
                            ${rec.reason ? `<p class="small text-info mb-2"><i class="bi bi-lightbulb"></i> ${escapeHtml(rec.reason)}</p>` : ''}
                        </div>
                    </div>
                    <button class="btn btn-sm btn-primary w-100 add-to-cart-btn" 
                            data-sku="${escapeHtml(rec.sku)}"
                            ${rec.inStock ? '' : 'disabled'}>
                        <i class="bi bi-cart-plus"></i> ${rec.inStock ? 'Add to Cart' : 'Out of Stock'}
                    </button>
                </div>
            `;
        });
        
        html += '</div>';
        recommendationsPanel.innerHTML = html;

        // Attach event listeners to add-to-cart buttons
        document.querySelectorAll('.add-to-cart-btn').forEach(btn => {
            btn.addEventListener('click', handleAddToCart);
        });
    }

    // Handle add to cart
    async function handleAddToCart(e) {
        const btn = e.currentTarget;
        const sku = btn.dataset.sku;
        
        btn.disabled = true;
        const originalText = btn.innerHTML;
        btn.innerHTML = '<span class="spinner-border spinner-border-sm"></span> Adding...';

        try {
            const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
            const formData = new FormData();
            formData.append('sku', sku);
            formData.append('__RequestVerificationToken', token);

            const response = await fetch('?handler=AddToCart', {
                method: 'POST',
                body: formData
            });

            const data = await response.json();

            if (data.success) {
                btn.innerHTML = '<i class="bi bi-check-circle"></i> Added!';
                btn.classList.remove('btn-primary');
                btn.classList.add('btn-success');
                
                // Show success message
                showToast('Product added to cart successfully!', 'success');
                
                setTimeout(() => {
                    btn.innerHTML = originalText;
                    btn.classList.remove('btn-success');
                    btn.classList.add('btn-primary');
                    btn.disabled = false;
                }, 2000);
            } else {
                throw new Error(data.error || 'Failed to add to cart');
            }
        } catch (error) {
            console.error('Add to cart error:', error);
            btn.innerHTML = originalText;
            btn.disabled = false;
            showToast('Failed to add product to cart', 'error');
        }
    }

    // Clear welcome message
    function clearWelcomeMessage() {
        const welcomeMsg = chatMessages.querySelector('.text-center.text-muted');
        if (welcomeMsg) {
            welcomeMsg.remove();
        }
    }

    // Set input state
    function setInputState(enabled) {
        userInput.disabled = !enabled;
        sendBtn.disabled = !enabled;
        
        if (!enabled) {
            sendBtn.innerHTML = '<span class="spinner-border spinner-border-sm"></span>';
        } else {
            sendBtn.innerHTML = '<i class="bi bi-send"></i> Send';
        }
    }

    // Escape HTML to prevent XSS
    function escapeHtml(unsafe) {
        if (typeof unsafe !== 'string') return unsafe;
        return unsafe
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#039;");
    }

    // Show toast notification
    function showToast(message, type = 'info') {
        // Simple toast implementation (you could use Bootstrap Toast for better UI)
        const toast = document.createElement('div');
        toast.className = `alert alert-${type === 'success' ? 'success' : 'danger'} position-fixed top-0 end-0 m-3`;
        toast.style.zIndex = '9999';
        toast.textContent = message;
        
        document.body.appendChild(toast);
        
        setTimeout(() => {
            toast.remove();
        }, 3000);
    }
})();
