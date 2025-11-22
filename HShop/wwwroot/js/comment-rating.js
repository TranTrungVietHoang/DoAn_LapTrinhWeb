// JavaScript for interactive star rating in product review form

document.addEventListener('DOMContentLoaded', function() {
    const ratingStars = document.getElementById('ratingStars');
    const ratingValue = document.getElementById('ratingValue');
    
    if (ratingStars && ratingValue) {
        const stars = ratingStars.querySelectorAll('.fa-star');
        let currentRating = 5; // Default rating
        
        // Set initial state (all stars filled)
        updateStars(currentRating);
        
        // Add click event to each star
        stars.forEach(star => {
            // Hover effect
            star.addEventListener('mouseenter', function() {
                const rating = parseInt(this.getAttribute('data-rating'));
                updateStars(rating);
            });
            
            // Click to select rating
            star.addEventListener('click', function() {
                currentRating = parseInt(this.getAttribute('data-rating'));
                ratingValue.value = currentRating;
                updateStars(currentRating);
            });
        });
        
        // Reset to current rating when mouse leaves
        ratingStars.addEventListener('mouseleave', function() {
            updateStars(currentRating);
        });
        
        function updateStars(rating) {
            stars.forEach((star, index) => {
                if (index < rating) {
                    star.classList.remove('text-muted');
                    star.classList.add('text-warning');
                } else {
                    star.classList.remove('text-warning');
                    star.classList.add('text-muted');
                }
            });
        }
    }
    
    // Image upload preview (optional enhancement)
    const imageUpload = document.getElementById('imageUpload');
    if (imageUpload) {
        imageUpload.addEventListener('change', function(e) {
            const file = e.target.files[0];
            if (file) {
                // Validate file size (5MB)
                if (file.size > 5 * 1024 * 1024) {
                    alert('Kích thước file không được vượt quá 5MB');
                    this.value = '';
                    return;
                }
                
                // Validate file type
                const allowedTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif'];
                if (!allowedTypes.includes(file.type)) {
                    alert('Chỉ chấp nhận file ảnh định dạng JPG, JPEG, PNG, GIF');
                    this.value = '';
                    return;
                }
            }
        });
    }
    
    // Form validation
    const reviewForm = document.getElementById('reviewForm');
    if (reviewForm) {
        reviewForm.addEventListener('submit', function(e) {
            const content = this.querySelector('textarea[name="Content"]').value.trim();
            
            if (content.length < 10) {
                e.preventDefault();
                alert('Nội dung đánh giá phải có ít nhất 10 ký tự');
                return false;
            }
            
            return true;
        });
    }
});
