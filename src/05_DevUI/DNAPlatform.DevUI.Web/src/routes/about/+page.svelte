<script lang="ts">
  import { translations } from './translations';
  
  let lang: 'en' | 'ar' = 'en';
  let isRTL = false;
  
  $: t = translations[lang];
  
  function toggleLang() {
    lang = lang === 'en' ? 'ar' : 'en';
    isRTL = lang === 'ar';
  }
</script>

<div class="about-page" dir={isRTL ? 'rtl' : 'ltr'}>
  <!-- Language Toggle -->
  <button class="lang-toggle" on:click={toggleLang}>
    {t.toggleLang}
  </button>
  
  <!-- Hero Section -->
  <section class="hero">
    <div class="dna-helix">🧬</div>
    <h1 class="hero-title">{t.title}</h1>
    <p class="hero-subtitle">{t.subtitle}</p>
  </section>
  
  <!-- Features Section -->
  <section class="features">
    <h2>{t.features.title}</h2>
    <div class="features-grid">
      {#each t.features.items as feature, i}
        <div class="feature-card" style="animation-delay: {i * 0.1}s">
          <span class="feature-icon">{feature.icon}</span>
          <h3>{feature.title}</h3>
          <p>{feature.desc}</p>
        </div>
      {/each}
    </div>
  </section>
  
  <!-- Tech Stack Section -->
  <section class="tech-stack">
    <h2>{t.techStack.title}</h2>
    <div class="tech-list">
      {#each t.techStack.items as tech}
        <span class="tech-badge">{tech}</span>
      {/each}
    </div>
  </section>
  
  <!-- Version Footer -->
  <footer class="version-footer">
    <p>{t.version}: <strong>v0.2.0-beta</strong></p>
  </footer>
</div>

<style>
  .about-page {
    max-width: 1200px;
    padding: 2rem;
    margin: 0 auto;
  }
  
  /* RTL Support */
  [dir="rtl"] .lang-toggle {
    right: auto;
    left: 1rem;
  }
  
  [dir="rtl"] .feature-card {
    text-align: right;
  }
  
  /* Language Toggle */
  .lang-toggle {
    position: fixed;
    top: 1rem;
    right: 1rem;
    background: var(--bg-card);
    border: 1px solid var(--border);
    color: var(--primary);
    padding: 0.5rem 1rem;
    border-radius: 2rem;
    cursor: pointer;
    font-size: 0.9rem;
    transition: all 0.2s ease;
    z-index: 100;
  }
  
  .lang-toggle:hover {
    background: var(--primary);
    color: white;
  }
  
  /* Hero Section */
  .hero {
    text-align: center;
    padding: 3rem 0;
  }
  
  .dna-helix {
    font-size: 5rem;
    animation: float 3s ease-in-out infinite;
    margin-bottom: 1rem;
  }
  
  @keyframes float {
    0%, 100% { transform: translateY(0); }
    50% { transform: translateY(-20px); }
  }
  
  .hero-title {
    font-size: 2.5rem;
    font-weight: 700;
    color: #f1f5f9;
    margin: 0 0 0.5rem 0;
    background: linear-gradient(135deg, var(--primary), #06b6d4);
    -webkit-background-clip: text;
    -webkit-text-fill-color: transparent;
    background-clip: text;
  }
  
  .hero-subtitle {
    font-size: 1.1rem;
    color: #94a3b8;
    margin: 0;
  }
  
  /* Features Section */
  .features {
    padding: 2rem 0;
  }
  
  .features h2 {
    font-size: 1.5rem;
    color: #e2e8f0;
    margin: 0 0 1.5rem 0;
  }
  
  .features-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
    gap: 1rem;
  }
  
  .feature-card {
    background: var(--bg-card);
    border: 1px solid var(--border);
    border-radius: 0.75rem;
    padding: 1.5rem;
    transition: all 0.3s ease;
    animation: fadeInUp 0.5s ease forwards;
    opacity: 0;
  }
  
  @keyframes fadeInUp {
    from {
      opacity: 0;
      transform: translateY(20px);
    }
    to {
      opacity: 1;
      transform: translateY(0);
    }
  }
  
  .feature-card:hover {
    transform: translateY(-5px);
    border-color: var(--primary);
    box-shadow: 0 10px 30px rgba(14, 165, 233, 0.2);
  }
  
  .feature-icon {
    font-size: 2rem;
    display: block;
    margin-bottom: 0.75rem;
  }
  
  .feature-card h3 {
    font-size: 1.1rem;
    color: #e2e8f0;
    margin: 0 0 0.5rem 0;
  }
  
  .feature-card p {
    font-size: 0.9rem;
    color: #94a3b8;
    margin: 0;
    line-height: 1.5;
  }
  
  /* Tech Stack Section */
  .tech-stack {
    padding: 2rem 0;
  }
  
  .tech-stack h2 {
    font-size: 1.5rem;
    color: #e2e8f0;
    margin: 0 0 1.5rem 0;
  }
  
  .tech-list {
    display: flex;
    flex-wrap: wrap;
    gap: 0.75rem;
  }
  
  .tech-badge {
    background: rgba(14, 165, 233, 0.1);
    border: 1px solid rgba(14, 165, 233, 0.3);
    color: var(--primary);
    padding: 0.5rem 1rem;
    border-radius: 2rem;
    font-size: 0.9rem;
    transition: all 0.2s ease;
  }
  
  .tech-badge:hover {
    background: rgba(14, 165, 233, 0.2);
    transform: scale(1.05);
  }
  
  /* Version Footer */
  .version-footer {
    text-align: center;
    padding: 2rem 0;
    margin-top: 2rem;
    border-top: 1px solid var(--border);
    color: #64748b;
  }
  
  .version-footer strong {
    color: var(--primary);
  }
  
  /* Responsive */
  @media (max-width: 768px) {
    .hero-title {
      font-size: 1.75rem;
    }
    
    .dna-helix {
      font-size: 3rem;
    }
    
    .features-grid {
      grid-template-columns: 1fr;
    }
  }
</style>