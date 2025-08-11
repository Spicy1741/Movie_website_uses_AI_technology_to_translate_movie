// Enhanced home-page.js with Three.js Integration

// Global variables for Three.js background system
let scene, camera, renderer, particles, mouse, raycaster;
let mouseX = 0, mouseY = 0;
let windowHalfX = window.innerWidth / 2;
let windowHalfY = window.innerHeight / 2;
let geometricShapes = [];

// Initialize everything when page loads
document.addEventListener('DOMContentLoaded', function () {
    // Initialize Three.js background
    initThreeJS();

    // Initialize existing slideshow functionality
    initSlideshow();

    // Initialize movie grid scrolling
    initMovieGridScrolling();

    // Initialize enhanced animations
    initEnhancedAnimations();

    // Initialize cursor effects
    initCursorEffects();

    // Hide loading screen after everything is ready
    setTimeout(() => {
        hideLoadingScreen();
        startPageAnimations();
    }, 2000);
});

// ========== THREE.JS BACKGROUND SYSTEM ==========
function initThreeJS() {
    // Create scene
    scene = new THREE.Scene();

    // Create camera
    camera = new THREE.PerspectiveCamera(75, window.innerWidth / window.innerHeight, 0.1, 1000);
    camera.position.z = 50;

    // Create renderer
    const canvas = document.getElementById('canvas');
    if (!canvas) return;

    renderer = new THREE.WebGLRenderer({
        canvas: canvas,
        antialias: true,
        alpha: true
    });
    renderer.setSize(window.innerWidth, window.innerHeight);
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
    renderer.setClearColor(0x000000, 1);

    // Create particles
    createParticles();

    // Create geometric shapes
    createGeometry();

    // Mouse and raycaster for interactions
    mouse = new THREE.Vector2();
    raycaster = new THREE.Raycaster();

    // Event listeners
    window.addEventListener('resize', onWindowResize);
    document.addEventListener('mousemove', onMouseMove);

    // Start animation loop
    animate();
}

function createParticles() {
    const geometry = new THREE.BufferGeometry();
    const vertices = [];
    const colors = [];

    for (let i = 0; i < 2000; i++) {
        vertices.push(
            Math.random() * 200 - 100,
            Math.random() * 200 - 100,
            Math.random() * 200 - 100
        );

        // Enhanced red color scheme for movie theme
        const intensity = Math.random();
        colors.push(
            0.8 + intensity * 0.2,  // Red
            intensity * 0.3,        // Green
            intensity * 0.2         // Blue
        );
    }

    geometry.setAttribute('position', new THREE.Float32BufferAttribute(vertices, 3));
    geometry.setAttribute('color', new THREE.Float32BufferAttribute(colors, 3));

    const material = new THREE.PointsMaterial({
        size: 0.5,
        vertexColors: true,
        transparent: true,
        opacity: 0.8
    });

    particles = new THREE.Points(geometry, material);
    scene.add(particles);
}

function createGeometry() {
    // Create floating geometric shapes
    geometricShapes = [];

    // Torus
    const torusGeometry = new THREE.TorusGeometry(10, 3, 16, 100);
    const torusMaterial = new THREE.MeshBasicMaterial({
        color: 0xdc2626,
        wireframe: true,
        transparent: true,
        opacity: 0.1
    });
    const torus = new THREE.Mesh(torusGeometry, torusMaterial);
    torus.position.set(-30, 20, -20);
    scene.add(torus);
    geometricShapes.push(torus);

    // Icosahedron
    const icoGeometry = new THREE.IcosahedronGeometry(8, 0);
    const icoMaterial = new THREE.MeshBasicMaterial({
        color: 0xffffff,
        wireframe: true,
        transparent: true,
        opacity: 0.15
    });
    const ico = new THREE.Mesh(icoGeometry, icoMaterial);
    ico.position.set(40, -30, -30);
    scene.add(ico);
    geometricShapes.push(ico);

    // Octahedron
    const octGeometry = new THREE.OctahedronGeometry(6, 0);
    const octMaterial = new THREE.MeshBasicMaterial({
        color: 0xdc2626,
        wireframe: true,
        transparent: true,
        opacity: 0.12
    });
    const oct = new THREE.Mesh(octGeometry, octMaterial);
    oct.position.set(0, 40, -40);
    scene.add(oct);
    geometricShapes.push(oct);

    // Additional geometric shapes for enhanced visual complexity
    const shapes = [
        { geometry: new THREE.BoxGeometry(4, 4, 4), position: [20, 30, -25] },
        { geometry: new THREE.SphereGeometry(3, 16, 16), position: [-40, -20, -35] },
        { geometry: new THREE.ConeGeometry(3, 8, 8), position: [35, 15, -45] },
        { geometry: new THREE.TetrahedronGeometry(5, 0), position: [-25, -35, -20] }
    ];

    shapes.forEach((shapeData, index) => {
        const material = new THREE.MeshBasicMaterial({
            color: index % 2 === 0 ? 0xdc2626 : 0xffffff,
            wireframe: true,
            transparent: true,
            opacity: 0.08 + Math.random() * 0.07
        });

        const mesh = new THREE.Mesh(shapeData.geometry, material);
        mesh.position.set(...shapeData.position);
        scene.add(mesh);
        geometricShapes.push(mesh);
    });

    // Animate shapes with GSAP
    if (typeof gsap !== 'undefined') {
        geometricShapes.forEach((shape, index) => {
            gsap.to(shape.rotation, {
                duration: 15 + index * 5,
                x: Math.PI * 2,
                y: Math.PI * 2,
                z: Math.PI * 2,
                repeat: -1,
                ease: "none"
            });
        });
    }
}

function onMouseMove(event) {
    mouseX = (event.clientX - windowHalfX) * 0.05;
    mouseY = (event.clientY - windowHalfY) * 0.05;

    mouse.x = (event.clientX / window.innerWidth) * 2 - 1;
    mouse.y = -(event.clientY / window.innerHeight) * 2 + 1;

    // Update cursor position
    const cursor = document.getElementById('cursor');
    if (cursor && window.innerWidth > 768) {
        cursor.style.left = event.clientX + 'px';
        cursor.style.top = event.clientY + 'px';
        cursor.style.opacity = '1';
    }
}

function onWindowResize() {
    windowHalfX = window.innerWidth / 2;
    windowHalfY = window.innerHeight / 2;

    if (camera && renderer) {
        camera.aspect = window.innerWidth / window.innerHeight;
        camera.updateProjectionMatrix();
        renderer.setSize(window.innerWidth, window.innerHeight);
    }
}

function animate() {
    requestAnimationFrame(animate);

    if (!camera || !renderer) return;

    // Rotate camera based on mouse
    camera.position.x += (mouseX - camera.position.x) * 0.02;
    camera.position.y += (-mouseY - camera.position.y) * 0.02;
    camera.lookAt(scene.position);

    // Rotate particles
    if (particles) {
        particles.rotation.x += 0.0005;
        particles.rotation.y += 0.001;
    }

    // Additional geometric shape animations
    geometricShapes.forEach((shape, index) => {
        const time = Date.now() * 0.001;
        shape.position.y += Math.sin(time + index) * 0.02;
        shape.position.x += Math.cos(time + index * 0.5) * 0.01;
    });

    renderer.render(scene, camera);
}

// ========== ENHANCED SLIDESHOW FUNCTIONALITY ==========
function initSlideshow() {
    const slideshow = document.getElementById('posterSlideshow');
    if (!slideshow) return;

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
    function initSlideshowFeatures() {
        if (slides.length <= 1) return;

        startAutoPlay();
        setupEventListeners();
    }

    // Show specific slide with enhanced 3D movement
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

        // Show current slide and indicator with enhanced animation
        slides[currentSlide].classList.add('active');
        if (indicators[currentSlide]) {
            indicators[currentSlide].classList.add('active');
        }

        // Update title display with animation
        const movieTitle = slides[currentSlide].getAttribute('data-movie-title');
        if (currentTitleDisplay && movieTitle) {
            // Animate title change
            if (typeof gsap !== 'undefined') {
                gsap.to(currentTitleDisplay, {
                    duration: 0.3,
                    opacity: 0,
                    y: -20,
                    onComplete: () => {
                        currentTitleDisplay.textContent = movieTitle;
                        gsap.to(currentTitleDisplay, {
                            duration: 0.3,
                            opacity: 1,
                            y: 0
                        });
                    }
                });
            } else {
                currentTitleDisplay.textContent = movieTitle;
            }
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
        }, 4000); // Enhanced timing
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

    // Enhanced hover auto-switching functionality
    function startHoverAutoSwitch() {
        if (slides.length <= 1) return;

        hoverAutoPlayInterval = setInterval(() => {
            if (isHovering) {
                nextSlide(); // Move to next slide (left to right)
            }
        }, 2500); // Enhanced timing for hover
    }

    function stopHoverAutoSwitch() {
        clearInterval(hoverAutoPlayInterval);
    }

    // Setup enhanced event listeners
    function setupEventListeners() {
        // Navigation buttons with enhanced effects
        if (prevBtn) {
            prevBtn.addEventListener('click', (e) => {
                e.stopPropagation();
                prevSlide();
                stopAutoPlay();
                setTimeout(resumeAutoPlay, 3000);

                // Enhanced button feedback
                if (typeof gsap !== 'undefined') {
                    gsap.to(prevBtn, {
                        duration: 0.1,
                        scale: 0.9,
                        onComplete: () => {
                            gsap.to(prevBtn, { duration: 0.1, scale: 1 });
                        }
                    });
                }
            });
        }

        if (nextBtn) {
            nextBtn.addEventListener('click', (e) => {
                e.stopPropagation();
                nextSlide();
                stopAutoPlay();
                setTimeout(resumeAutoPlay, 3000);

                // Enhanced button feedback
                if (typeof gsap !== 'undefined') {
                    gsap.to(nextBtn, {
                        duration: 0.1,
                        scale: 0.9,
                        onComplete: () => {
                            gsap.to(nextBtn, { duration: 0.1, scale: 1 });
                        }
                    });
                }
            });
        }

        // Enhanced indicator buttons
        indicators.forEach((indicator, index) => {
            indicator.addEventListener('click', (e) => {
                e.stopPropagation();
                const direction = index > currentSlide ? 'next' : 'prev';
                showSlide(index, direction);
                stopAutoPlay();
                setTimeout(resumeAutoPlay, 3000);

                // Enhanced indicator feedback
                if (typeof gsap !== 'undefined') {
                    gsap.to(indicator, {
                        duration: 0.2,
                        scale: 1.3,
                        onComplete: () => {
                            gsap.to(indicator, { duration: 0.2, scale: 1.3 });
                        }
                    });
                }
            });
        });

        // Enhanced hover effects with 3D transformations
        slideshow.addEventListener('mouseenter', () => {
            isHovering = true;
            stopAutoPlay();

            // Enhanced 3D hover effect
            if (typeof gsap !== 'undefined') {
                gsap.to(slideshow, {
                    duration: 0.6,
                    rotationY: -2,
                    rotationX: 1,
                    transformPerspective: 1500,
                    ease: "power2.out"
                });
            }

            // Start automatic switching on hover after delay
            setTimeout(() => {
                if (isHovering) {
                    startHoverAutoSwitch();
                }
            }, 1000);
        });

        slideshow.addEventListener('mouseleave', () => {
            isHovering = false;
            stopHoverAutoSwitch();
            resumeAutoPlay();

            // Reset 3D transformation
            if (typeof gsap !== 'undefined') {
                gsap.to(slideshow, {
                    duration: 0.6,
                    rotationY: 0,
                    rotationX: 0,
                    ease: "power2.out"
                });
            }
        });

        // Enhanced click navigation
        slideshow.addEventListener('click', (e) => {
            if (!e.target.closest('.slideshow-controls') &&
                !e.target.closest('.slide-indicators') &&
                !e.target.closest('.movie-tooltip')) {
                const activeSlide = slideshow.querySelector('.poster-slide.active');
                if (activeSlide) {
                    const movieId = activeSlide.getAttribute('data-movie-id');
                    if (movieId) {
                        // Enhanced click feedback
                        if (typeof gsap !== 'undefined') {
                            gsap.to(slideshow, {
                                duration: 0.2,
                                scale: 0.98,
                                onComplete: () => {
                                    window.location.href = `/Movie/Details?id=${movieId}`;
                                }
                            });
                        } else {
                            window.location.href = `/Movie/Details?id=${movieId}`;
                        }
                    }
                }
            }
        });

        // Enhanced touch/swipe support for mobile
        let touchStartX = 0;
        let touchEndX = 0;
        let touchStartY = 0;
        let touchEndY = 0;

        slideshow.addEventListener('touchstart', (e) => {
            touchStartX = e.changedTouches[0].screenX;
            touchStartY = e.changedTouches[0].screenY;
            isHovering = true;
            stopAutoPlay();
            stopHoverAutoSwitch();
        });

        slideshow.addEventListener('touchend', (e) => {
            touchEndX = e.changedTouches[0].screenX;
            touchEndY = e.changedTouches[0].screenY;
            handleSwipe();
            isHovering = false;
            setTimeout(resumeAutoPlay, 2000);
        });

        function handleSwipe() {
            const swipeThreshold = 50;
            const swipeDistanceX = touchEndX - touchStartX;
            const swipeDistanceY = Math.abs(touchEndY - touchStartY);

            // Only handle horizontal swipes
            if (Math.abs(swipeDistanceX) > swipeThreshold && swipeDistanceY < 100) {
                if (swipeDistanceX > 0) {
                    prevSlide(); // Swipe right - previous slide
                } else {
                    nextSlide(); // Swipe left - next slide
                }
            }
        }

        // Enhanced keyboard navigation
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
                        const activeSlide = slideshow.querySelector('.poster-slide.active');
                        if (activeSlide) {
                            const movieId = activeSlide.getAttribute('data-movie-id');
                            if (movieId) {
                                window.location.href = `/Movie/Details?id=${movieId}`;
                            }
                        }
                        break;
                }
            }
        });
    }

    // Initialize slideshow
    initSlideshowFeatures();
}

// ========== ENHANCED MOVIE GRID SCROLLING ==========
function initMovieGridScrolling() {
    const prevBtns = document.querySelectorAll('.prev-btn');
    const nextBtns = document.querySelectorAll('.next-btn');
    const scrollAmount = 240; // Enhanced scroll amount

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

                // Enhanced button feedback
                if (typeof gsap !== 'undefined') {
                    gsap.to(this, {
                        duration: 0.15,
                        scale: 0.9,
                        rotation: -5,
                        onComplete: () => {
                            gsap.to(this, {
                                duration: 0.15,
                                scale: 1,
                                rotation: 0
                            });
                        }
                    });
                }
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

                // Enhanced button feedback
                if (typeof gsap !== 'undefined') {
                    gsap.to(this, {
                        duration: 0.15,
                        scale: 0.9,
                        rotation: 5,
                        onComplete: () => {
                            gsap.to(this, {
                                duration: 0.15,
                                scale: 1,
                                rotation: 0
                            });
                        }
                    });
                }
            }
        });
    });

    // Enhanced movie card interactions
    const movieCards = document.querySelectorAll('.movie-card');
    movieCards.forEach((card, index) => {
        // Enhanced hover effects with 3D transformations
        card.addEventListener('mouseenter', function () {
            if (typeof gsap !== 'undefined') {
                gsap.to(this, {
                    duration: 0.4,
                    y: -20,
                    rotationY: -8,
                    rotationX: 3,
                    scale: 1.05,
                    ease: "power2.out"
                });

                // Animate the glow effect
                gsap.to(this.querySelector('::before'), {
                    duration: 0.4,
                    opacity: 1,
                    ease: "power2.out"
                });
            }
        });

        card.addEventListener('mouseleave', function () {
            if (typeof gsap !== 'undefined') {
                gsap.to(this, {
                    duration: 0.4,
                    y: 0,
                    rotationY: 0,
                    rotationX: 0,
                    scale: 1,
                    ease: "power2.out"
                });
            }
        });

        // Enhanced click interaction
        card.addEventListener('click', function (e) {
            if (!e.target.closest('.play-overlay')) {
                const movieId = this.getAttribute('data-movie-id');
                if (movieId) {
                    // Enhanced click feedback
                    if (typeof gsap !== 'undefined') {
                        gsap.to(this, {
                            duration: 0.1,
                            scale: 0.95,
                            onComplete: () => {
                                window.location.href = `/Movie/Details?id=${movieId}`;
                            }
                        });
                    } else {
                        window.location.href = `/Movie/Details?id=${movieId}`;
                    }
                }
            }
        });
    });
}

// ========== ENHANCED ANIMATIONS WITH GSAP ==========
function initEnhancedAnimations() {
    if (typeof gsap === 'undefined') return;

    // Register ScrollTrigger plugin
    gsap.registerPlugin(ScrollTrigger);

    // Enhanced section title animations
    gsap.utils.toArray('.section-title').forEach((title, i) => {
        gsap.fromTo(title,
            {
                opacity: 0,
                y: 100,
                rotationX: 90
            },
            {
                opacity: 1,
                y: 0,
                rotationX: 0,
                duration: 0.5,
                ease: "power3.out",
                scrollTrigger: {
                    trigger: title,
                    start: "top 85%",
                    end: "bottom 20%",
                    toggleActions: "play none none reverse"
                }
            }
        );
    });

    // Enhanced movie card animations with stagger
    gsap.utils.toArray('.movie-card').forEach((card, i) => {
        gsap.fromTo(card,
            {
                opacity: 0,
                y: 100,
                scale: 0.8,
                rotationY: 45
            },
            {
                opacity: 1,
                y: 0,
                scale: 1,
                rotationY: 0,
                duration: 0.3,
                delay: i * 0.1,
                ease: "power3.out",
                scrollTrigger: {
                    trigger: card,
                    start: "top 90%",
                    toggleActions: "play none none reverse"
                }
            }
        );
    });

    // Enhanced feature items animation for welcome section
    gsap.utils.toArray('.feature-item').forEach((item, i) => {
        gsap.fromTo(item,
            {
                opacity: 0,
                y: 50,
                scale: 0.9
            },
            {
                opacity: 1,
                y: 0,
                scale: 1,
                duration: 0.6,
                delay: i * 0.2,
                ease: "power2.out",
                scrollTrigger: {
                    trigger: item,
                    start: "top 85%",
                    toggleActions: "play none none reverse"
                }
            }
        );
    });

    // Enhanced parallax effects for Three.js camera
    gsap.to(camera ? camera.position : {}, {
        z: 30,
        ease: "none",
        scrollTrigger: {
            trigger: "body",
            start: "top top",
            end: "bottom bottom",
            scrub: 1
        }
    });
}

// ========== ENHANCED CURSOR EFFECTS ==========
function initCursorEffects() {
    const cursor = document.getElementById('cursor');
    if (!cursor || window.innerWidth <= 768) return;

    // Enhanced cursor interactions
    const interactiveElements = [
        '.movie-card',
        '.tooltip-btn',
        '.watch-btn',
        '.control-btn',
        '.slide-control',
        '.slide-indicator',
        '.welcome-btn',
        '.action-btn',
        'a',
        'button'
    ];

    interactiveElements.forEach(selector => {
        document.addEventListener('mouseenter', (e) => {
            if (e.target.matches(selector) || e.target.closest(selector)) {
                cursor.classList.add('hover');
                if (typeof gsap !== 'undefined') {
                    gsap.to(cursor, {
                        duration: 0.2,
                        scale: 2,
                        ease: "power2.out"
                    });
                }
            }
        }, true);

        document.addEventListener('mouseleave', (e) => {
            if (e.target.matches(selector) || e.target.closest(selector)) {
                cursor.classList.remove('hover');
                if (typeof gsap !== 'undefined') {
                    gsap.to(cursor, {
                        duration: 0.2,
                        scale: 1,
                        ease: "power2.out"
                    });
                }
            }
        }, true);
    });

    // Enhanced cursor movement
    document.addEventListener('mousemove', (e) => {
        if (typeof gsap !== 'undefined') {
            gsap.to(cursor, {
                duration: 0.1,
                x: e.clientX,
                y: e.clientY,
                ease: "power2.out"
            });
        } else {
            cursor.style.left = e.clientX + 'px';
            cursor.style.top = e.clientY + 'px';
        }
        cursor.style.opacity = '1';
    });
}

// ========== PAGE LOADING AND ANIMATION STARTUP ==========
function hideLoadingScreen() {
    const loading = document.getElementById('loading');
    if (loading) {
        if (typeof gsap !== 'undefined') {
            gsap.to(loading, {
                duration: 1,
                opacity: 0,
                ease: "power2.out",
                onComplete: () => {
                    loading.classList.add('hidden');
                }
            });
        } else {
            loading.classList.add('hidden');
        }
    }
}

function startPageAnimations() {
    if (typeof gsap === 'undefined') return;

    // Enhanced hero section animations
    const heroTitle = document.querySelector('.welcome-title');
    const heroSubtitle = document.querySelector('.welcome-subtitle');
    const heroButtons = document.querySelectorAll('.welcome-btn');

    if (heroTitle) {
        gsap.fromTo(heroTitle,
            {
                opacity: 0,
                y: 100,
                scale: 0.8
            },
            {
                opacity: 1,
                y: 0,
                scale: 1,
                duration: 1.2,
                ease: "power3.out"
            }
        );
    }

    if (heroSubtitle) {
        gsap.fromTo(heroSubtitle,
            {
                opacity: 0,
                y: 50
            },
            {
                opacity: 1,
                y: 0,
                duration: 1,
                delay: 0.3,
                ease: "power2.out"
            }
        );
    }

    heroButtons.forEach((btn, i) => {
        gsap.fromTo(btn,
            {
                opacity: 0,
                y: 30,
                scale: 0.9
            },
            {
                opacity: 1,
                y: 0,
                scale: 1,
                duration: 0.8,
                delay: 0.5 + (i * 0.1),
                ease: "power2.out"
            }
        );
    });

    // Enhanced slideshow entrance animation
    const slideshow = document.getElementById('posterSlideshow');
    if (slideshow) {
        gsap.fromTo(slideshow,
            {
                opacity: 0,
                scale: 0.8,
                y: 100
            },
            {
                opacity: 1,
                scale: 1,
                y: 0,
                duration: 1.5,
                delay: 0.5,
                ease: "power3.out"
            }
        );
    }
}

// ========== UTILITY FUNCTIONS ==========

// Enhanced debounce function
function debounce(func, wait, immediate) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            timeout = null;
            if (!immediate) func(...args);
        };
        const callNow = immediate && !timeout;
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
        if (callNow) func(...args);
    };
}

// Enhanced smooth scroll function
function smoothScrollTo(element, duration = 1000) {
    if (typeof gsap !== 'undefined') {
        gsap.to(window, {
            duration: duration / 1000,
            scrollTo: element,
            ease: "power2.out"
        });
    } else {
        element.scrollIntoView({
            behavior: 'smooth',
            block: 'start'
        });
    }
}

// Enhanced visibility check
function isElementInViewport(el) {
    const rect = el.getBoundingClientRect();
    return (
        rect.top >= 0 &&
        rect.left >= 0 &&
        rect.bottom <= (window.innerHeight || document.documentElement.clientHeight) &&
        rect.right <= (window.innerWidth || document.documentElement.clientWidth)
    );
}

// Performance optimization
const observerOptions = {
    root: null,
    rootMargin: '50px',
    threshold: 0.1
};

const intersectionObserver = new IntersectionObserver((entries) => {
    entries.forEach(entry => {
        if (entry.isIntersecting) {
            entry.target.classList.add('animate');
        }
    });
}, observerOptions);

// Observe elements for animation
document.querySelectorAll('.movie-card, .section-title, .feature-item').forEach(el => {
    intersectionObserver.observe(el);
});

// Console message
console.log('🎬 Enhanced CineHub Home Page with Three.js Background Ready! 🎬');