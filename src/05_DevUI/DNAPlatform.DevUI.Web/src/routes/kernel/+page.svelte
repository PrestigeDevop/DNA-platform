<script lang="ts">
  import { onMount } from 'svelte';
  import { goto } from '$app/navigation';

  onMount(() => {
    goto('/polyglot');
  });
</script>

<div class="redirect-box">
  <p>Redirecting to dedicated <a href="/polyglot">Polyglot Notebook</a>...</p>
</div>

<style>
  .redirect-box {
    padding: 2rem;
    text-align: center;
    color: #94a3b8;
  }
  a {
    color: #38bdf8;
  }
</style>
