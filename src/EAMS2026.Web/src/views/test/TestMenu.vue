<template>
  <div class="test-menu">
    <h2>测试菜单</h2>
    <el-alert title="此菜单用于测试权限管理功能" type="info" :closable="false" style="margin-top: 16px;" />
    <el-card style="margin-top: 20px;">
      <template #header>权限测试说明</template>
      <p>请从左侧子菜单进入权限1子菜单和权限2子菜单进行测试。</p>
    </el-card>
  </div>
</template>

<script setup lang="ts">
</script>

<style scoped>
.test-menu { padding: 10px; }
</style>