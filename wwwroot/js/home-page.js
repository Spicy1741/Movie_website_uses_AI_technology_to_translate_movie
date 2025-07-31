document.addEventListener('DOMContentLoaded', function () {
    // Enhanced Poster-Only Slideshow Functionality
    const slideshow = document.getElementById('posterSlideshow');
    if (slideshow) {
        const slides = slideshow.querySelectorAll('.poster-slide');
        const indicators = slideshow.querySelectorAll('.slide-indicator');
        const prevBtn = document.getElementById('prevSlide');
        const nextBtn = document.getElementById('nextSlide');
        const currentTitleDisplay = document.getElementById('currentMovieTitle');

        let currentSlide = 0;
        let isAutoPlaying = true;
        let autoPlayInterval;
        let hoverAutoPlayInterval;
        let isHovering = false;

        // Initialize slideshow
        function initSlideshow() {
            if (slides.length <= 1) return;

            startAutoPlay();
            setupEventListeners();
        }

        // Show specific slide with predictable left-to-right movement
        function showSlide(index, direction = 'next') {
            if (slides.length <= 1) return;

            // Remove all classes from all slides
            slides.forEach(slide => {
                slide.classList.remove('active', 'prev', 'next');
            });
            indicators.forEach(indicator => {
                indicator.classList.remove('active');
            });

            // Set the previous slide with appropriate direction
            if (currentSlide !== index) {
                if (direction === 'next') {
                    slides[currentSlide].classList.add('prev');
                } else {
                    slides[currentSlide].classList.add('next');
                }
            }

            // Update current slide
            currentSlide = index;

            // Show current slide and indicator
            slides[currentSlide].classList.add('active');
            if (indicators[currentSlide]) {
                indicators[currentSlide].classList.add('active');
            }

            // Update title display
            const movieTitle = slides[currentSlide].getAttribute('data-movie-title');
            if (currentTitleDisplay && movieTitle) {
                currentTitleDisplay.textContent = movieTitle;
            }
        }

        // Next slide (left to right)
        function nextSlide() {
            const next = (currentSlide + 1) % slides.length;
            showSlide(next, 'next');
        }

        // Previous slide (right to left)
        function prevSlide() {
            const prev = (currentSlide - 1 + slides.length) % slides.length;
            showSlide(prev, 'prev');
        }

        // Auto play functionality
        function startAutoPlay() {
            if (slides.length <= 1) return;

            autoPlayInterval = setInterval(() => {
                if (isAutoPlaying && !isHovering) {
                    nextSlide();
                }
            }, 3500); // Consistent 3.5 second intervals
        }

        function stopAutoPlay() {
            clearInterval(autoPlayInterval);
            isAutoPlaying = false;
        }

        function resumeAutoPlay() {
            stopAutoPlay();
            isAutoPlaying = true;
            startAutoPlay();
        }

        // Hover auto-switching functionality (left to right)
        function startHoverAutoSwitch() {
            if (slides.length <= 1) return;

            hoverAutoPlayInterval = setInterval(() => {
                if (isHovering) {
                    nextSlide(); // Move to next slide (left to right)
                }
            }, 2000); // Consistent 1.2 second intervals on hover
        }

        function stopHoverAutoSwitch() {
            clearInterval(hoverAutoPlayInterval);
        }

        // Setup event listeners
        function setupEventListeners() {
            // Navigation buttons
            if (prevBtn) {
                prevBtn.addEventListener('click', (e) => {
                    e.stopPropagation();
                    prevSlide();
                    stopAutoPlay();
                    setTimeout(resumeAutoPlay, 3000);
                });
            }

            if (nextBtn) {
                nextBtn.addEventListener('click', (e) => {
                    e.stopPropagation();
                    nextSlide();
                    stopAutoPlay();
                    setTimeout(resumeAutoPlay, 3000);
                });
            }

            // Indicator buttons
            indicators.forEach((indicator, index) => {
                indicator.addEventListener('click', (e) => {
                    e.stopPropagation();
                    const direction = index > currentSlide ? 'next' : 'prev';
                    showSlide(index, direction);
                    stopAutoPlay();
                    setTimeout(resumeAutoPlay, 3000);
                });
            });

            // Enhanced hover effects - automatic switching
            slideshow.addEventListener('mouseenter', () => {
                isHovering = true;
                stopAutoPlay();

                // Start automatic switching on hover after a brief delay
                setTimeout(() => {
                    if (isHovering) {
                        startHoverAutoSwitch();
                    }
                }, 800); // Wait 0.8 seconds before starting hover auto-switch
            });

            slideshow.addEventListener('mouseleave', () => {
                isHovering = false;
                stopHoverAutoSwitch();
                resumeAutoPlay();
            });

            // Click on slideshow to go to movie details
            slideshow.addEventListener('click', (e) => {
                // Don't navigate if clicking on controls
                if (!e.target.closest('.slideshow-controls') &&
                    !e.target.closest('.slide-indicators') &&
                    !e.target.closest('.movie-tooltip')) {
                    const activeSlide = slideshow.querySelector('.poster-slide.active');
                    if (activeSlide) {
                        const movieId = activeSlide.getAttribute('data-movie-id');
                        if (movieId) {
                            window.location.href = `@Url.Action("Details", "Movie")?id=${movieId}`;
                        }
                    }
                }
            });

            // Touch/swipe support for mobile
            let touchStartX = 0;
            let touchEndX = 0;

            slideshow.addEventListener('touchstart', (e) => {
                touchStartX = e.changedTouches[0].screenX;
                isHovering = true;
                stopAutoPlay();
                stopHoverAutoSwitch();
            });

            slideshow.addEventListener('touchend', (e) => {
                touchEndX = e.changedTouches[0].screenX;
                handleSwipe();
                isHovering = false;
                setTimeout(resumeAutoPlay, 2000);
            });

            function handleSwipe() {
                const swipeThreshold = 50;
                const swipeDistance = touchEndX - touchStartX;

                if (Math.abs(swipeDistance) > swipeThreshold) {
                    if (swipeDistance > 0) {
                        prevSlide(); // Swipe right - previous slide
                    } else {
                        nextSlide(); // Swipe left - next slide
                    }
                }
            }

            // Keyboard navigation
            document.addEventListener('keydown', (e) => {
                if (document.activeElement === slideshow || slideshow.contains(document.activeElement)) {
                    switch (e.key) {
                        case 'ArrowLeft':
                            e.preventDefault();
                            prevSlide();
                            stopAutoPlay();
                            setTimeout(resumeAutoPlay, 3000);
                            break;
                        case 'ArrowRight':
                            e.preventDefault();
                            nextSlide();
                            stopAutoPlay();
                            setTimeout(resumeAutoPlay, 3000);
                            break;
                        case 'Enter':
                        case ' ':
                            e.preventDefault();
                            // Navigate to movie details
                            const activeSlide = slideshow.querySelector('.poster-slide.active');
                            if (activeSlide) {
                                const movieId = activeSlide.getAttribute('data-movie-id');
                                if (movieId) {
                                    window.location.href = `@Url.Action("Details", "Movie")?id=${movieId}`;
                                }
                            }
                            break;
                    }
                }
            });
        }

        // Initialize
        initSlideshow();
    }

    // Horizontal Movie Scroll Functionality (existing code)
    const prevBtns = document.querySelectorAll('.prev-btn');
    const nextBtns = document.querySelectorAll('.next-btn');
    const scrollAmount = 220;

    prevBtns.forEach(btn => {
        btn.addEventListener('click', function () {
            const category = this.getAttribute('data-category');
            const gridId = 'grid-' + category.replace(/\s+/g, '-').toLowerCase();
            const movieGrid = document.getElementById(gridId);

            if (movieGrid) {
                movieGrid.scrollBy({
                    left: -scrollAmount,
                    behavior: 'smooth'
                });

                this.style.transform = 'scale(0.95)';
                setTimeout(() => {
                    this.style.transform = 'scale(1)';
                }, 150);
            }
        });
    });

    nextBtns.forEach(btn => {
        btn.addEventListener('click', function () {
            const category = this.getAttribute('data-category');
            const gridId = 'grid-' + category.replace(/\s+/g, '-').toLowerCase();
            const movieGrid = document.getElementById(gridId);

            if (movieGrid) {
                movieGrid.scrollBy({
                    left: scrollAmount,
                    behavior: 'smooth'
                });

                this.style.transform = 'scale(0.95)';
                setTimeout(() => {
                    this.style.transform = 'scale(1)';
                }, 150);
            }
        });
    });

    // Additional functionality for authenticated users
    @if (User.Identity!.IsAuthenticated) {
        <text>
                // Movie card interactions
            const movieCards = document.querySelectorAll('.movie-card');
                movieCards.forEach(card => {
                card.addEventListener('click', function (e) {
                    if (!e.target.closest('.play-overlay')) {
                        const movieId = this.getAttribute('data-movie-id');
                        window.location.href = `@Url.Action("Details", "Movie")?id=${movieId}`;
                    }
                });
                });

            // Search functionality
            const searchBtn = document.querySelector('.search-btn');
            if (searchBtn) {
                searchBtn.addEventListener('click', function () {
                    window.location.href = '@Url.Action("Index", "Movie")';
                });
                }

            // Notification functionality
            const notificationBtn = document.querySelector('.notification-btn');
            if (notificationBtn) {
                notificationBtn.addEventListener('click', function () {
                    alert('Notifications feature coming soon!');
                });
                }
        </text>
    }
    else {
        <text>
                // Welcome section animations
            const welcomeIcon = document.querySelector('.welcome-icon');
            const featureItems = document.querySelectorAll('.feature-item');

                featureItems.forEach((item, index) => {
                item.style.opacity = '0';
            item.style.transform = 'translateY(20px)';
                    setTimeout(() => {
                item.style.transition = 'all 0.6s ease';
            item.style.opacity = '1';
            item.style.transform = 'translateY(0)';
                    }, 500 + (index * 200));
                });
        </text>
    }
});