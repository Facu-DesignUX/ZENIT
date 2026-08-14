window.ZenitAudioPlayer = {
    audio: null,
    audioContext: null,
    gainNode: null,
    source: null,
    dotNetRef: null,

    initialize: function (dotNetReference) {
        this.dotNetRef = dotNetReference;
        
        // Grab or create the audio element
        this.audio = document.getElementById('zenit-audio');
        if (!this.audio) {
            this.audio = document.createElement('audio');
            this.audio.id = 'zenit-audio';
            this.audio.crossOrigin = 'anonymous'; // Important for Web Audio API
            document.body.appendChild(this.audio);
        }

        // Initialize Web Audio API for Gain Control (up to 200%)
        if (!this.audioContext) {
            const AudioContext = window.AudioContext || window.webkitAudioContext;
            this.audioContext = new AudioContext();
            this.source = this.audioContext.createMediaElementSource(this.audio);
            this.gainNode = this.audioContext.createGain();
            this.gainNode.gain.value = 1.0;
            this.source.connect(this.gainNode);
            this.gainNode.connect(this.audioContext.destination);
            
            // Resume context on play to comply with autoplay policies
            this.audio.addEventListener('play', () => {
                if (this.audioContext && this.audioContext.state === 'suspended') {
                    this.audioContext.resume();
                }
            });
        }

        // Attach event listeners using direct assignment to prevent duplicate listeners on hot reload
        this.audio.onplaying = () => {
            this.isPlaying = true;
            this.dotNetRef.invokeMethodAsync('OnAudioPlaying').catch(e => console.warn(e));
        };

        this.audio.onpause = () => {
            this.isPlaying = false;
            this.dotNetRef.invokeMethodAsync('OnAudioPaused').catch(e => console.warn(e));
        };

        this.audio.onwaiting = () => {
            this.isPlaying = false;
            this.dotNetRef.invokeMethodAsync('OnAudioWaiting').catch(e => console.warn(e));
        };

        this.audio.onerror = (e) => {
            console.error("Audio playback error", e);
            this.isPlaying = false;
            this.dotNetRef.invokeMethodAsync('OnAudioError').catch(e => console.warn(e));
        };

        // Failsafe: if time updates and we were waiting, we are playing again
        this.audio.ontimeupdate = () => {
            if (this.audio && !this.audio.paused && !this.isPlaying) {
                this.isPlaying = true;
                this.dotNetRef.invokeMethodAsync('OnAudioPlaying').catch(e => console.warn(e));
            }
        };

        // Setup Media Session API handlers (hardware buttons, lock screen)
        if ('mediaSession' in navigator) {
            navigator.mediaSession.setActionHandler('play', () => {
                this.audio.play();
            });
            navigator.mediaSession.setActionHandler('pause', () => {
                this.audio.pause();
            });
        }
    },

    play: function (url, metadata) {
        if (!this.audio) return;
        
        this.audio.src = url;
        this.audio.load();
        
        // Use a promise to catch play interruptions
        const playPromise = this.audio.play();
        if (playPromise !== undefined) {
            playPromise.catch(error => {
                console.error("Autoplay prevented or network error:", error);
                this.dotNetRef.invokeMethodAsync('OnAudioError');
            });
        }

        // Update lock screen metadata
        if ('mediaSession' in navigator && metadata) {
            navigator.mediaSession.metadata = new MediaMetadata({
                title: metadata.title || 'Unknown Station',
                artist: metadata.artist || 'ZENIT',
                album: metadata.album || 'Radio',
                artwork: [
                    { src: metadata.artworkUrl || '/icon-512.png', sizes: '512x512', type: 'image/png' }
                ]
            });
        }
    },

    pause: function () {
        if (this.audio) {
            this.audio.pause();
        }
    },

    resume: function () {
        if (this.audio) {
            this.audio.play();
        }
    },

    setVolume: function (volume) {
        if (this.gainNode) {
            // volume can be up to 2.0 (200%)
            this.gainNode.gain.value = Math.max(0, Math.min(2.0, volume));
        } else if (this.audio) {
            this.audio.volume = Math.max(0, Math.min(1.0, volume));
        }
    }
};
