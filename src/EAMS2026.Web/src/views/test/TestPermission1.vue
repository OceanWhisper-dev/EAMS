<template>
  <div class="test-permission1">
    <h2>权限1子菜单</h2>
    <el-card style="margin-top: 20px;">
      <template #header>功能测试区域</template>
      <el-space wrap>
        <el-button v-permission="'test:func11'" type="primary">功能11（权限: test:func11）</el-button>
        <el-button v-permission="'test:func12'" type="success">功能12（权限: test:func12）</el-button>
      </el-space>
    </el-card>
  </div>
</template>

<script setup lang="ts">
</script>

<style scoped>
.test-permission1 { padding: 10px; }
</style>