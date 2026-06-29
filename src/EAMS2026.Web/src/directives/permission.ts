import type { Directive, DirectiveBinding } from 'vue'
import { useAuthStore } from '@/stores/auth'

export const permission: Directive = {
  mounted(el: HTMLElement, binding: DirectiveBinding<string>) {
    const authStore = useAuthStore()
    const value = binding.value
    if (value && !authStore.hasPermission(value)) {
      el.style.display = 'none'
      el.setAttribute('data-permission-hidden', 'true')
    }
  },
  updated(el: HTMLElement, binding: DirectiveBinding<string>) {
    const authStore = useAuthStore()
    const value = binding.value
    if (value && !authStore.hasPermission(value)) {
      el.style.display = 'none'
    } else {
      el.style.display = ''
      el.removeAttribute('data-permission-hidden')
    }
  }
}