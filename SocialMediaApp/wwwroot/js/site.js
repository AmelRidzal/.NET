document.addEventListener('DOMContentLoaded', function() {
    // Handle like button clicks
    document.addEventListener('click', function(e) {
        if (e.target.closest('.like-button')) {
            e.preventDefault();
            const button = e.target.closest('.like-button');
            const postId = button.dataset.postId;
            
            // Disable button during request
            button.disabled = true;
            
            // Get anti-forgery token
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            
            // Make AJAX request
            fetch('/Posts/ToggleLike', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                    'X-Requested-With': 'XMLHttpRequest',
                    'RequestVerificationToken': token
                },
                body: `postId=${postId}`
            })
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    // Update the like icon
                    const likeIcon = button.querySelector('.like-icon');
                    if (data.isLiked) {
                        likeIcon.innerHTML = '<span class="text-primary">❤️</span>';
                    } else {
                        likeIcon.innerHTML = '<span class="text-muted">🤍</span>';
                    }
                    
                    // Update the like count
                    const likeCount = button.querySelector('.like-count');
                    likeCount.textContent = data.likesCount;
                    
                    // Update the data attribute
                    button.dataset.isLiked = data.isLiked;
                }
            })
            .catch(error => {
                console.error('Error:', error);
                alert('Failed to update like. Please try again.');
            })
            .finally(() => {
                // Re-enable button
                button.disabled = false;
            });
        }
    });

    // Handle comment form submissions
    document.addEventListener('submit', function(e) {
        if (e.target.classList.contains('comment-form')) {
            e.preventDefault();
            const form = e.target;
            const postId = form.dataset.postId;
            const input = form.querySelector('.comment-input');
            const content = input.value.trim();
            
            if (!content) {
                alert('Please enter a comment.');
                return;
            }
            
            // Disable form during request
            const submitButton = form.querySelector('button[type="submit"]');
            submitButton.disabled = true;
            input.disabled = true;
            
            // Get anti-forgery token
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            
            // Make AJAX request
            fetch('/Posts/AddComment', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                    'X-Requested-With': 'XMLHttpRequest',
                    'RequestVerificationToken': token
                },
                body: `postId=${postId}&content=${encodeURIComponent(content)}`
            })
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    // Get the comments list
                    const commentsList = document.getElementById(`comments-list-${postId}`);
                    
                    // Remove "no comments" message if it exists
                    const noCommentsMsg = commentsList.querySelector('.no-comments-message');
                    if (noCommentsMsg) {
                        noCommentsMsg.remove();
                    }
                    
                    // Create new comment element
                    const commentDiv = document.createElement('div');
                    commentDiv.className = 'comment border-bottom py-2';
                    commentDiv.innerHTML = `
                        <span class="fw-bold">${data.comment.userName}</span>
                        <small class="text-muted ms-2">
                            ${data.comment.createdAt}
                        </small>
                        <div>${data.comment.content}</div>
                    `;
                    
                    // Append new comment to the list
                    commentsList.appendChild(commentDiv);
                    
                    // Update comment count
                    const commentCountElement = document.querySelector(`.comment-count-${postId}`);
                    if (commentCountElement) {
                        commentCountElement.textContent = data.commentsCount;
                    }
                    
                    // Clear the input
                    input.value = '';
                    
                    // Optional: Add a subtle animation to the new comment
                    commentDiv.style.opacity = '0';
                    setTimeout(() => {
                        commentDiv.style.transition = 'opacity 0.3s';
                        commentDiv.style.opacity = '1';
                    }, 10);
                } else {
                    alert(data.message || 'Failed to add comment.');
                }
            })
            .catch(error => {
                console.error('Error:', error);
                alert('Failed to add comment. Please try again.');
            })
            .finally(() => {
                // Re-enable form
                submitButton.disabled = false;
                input.disabled = false;
            });
        }
    });
});