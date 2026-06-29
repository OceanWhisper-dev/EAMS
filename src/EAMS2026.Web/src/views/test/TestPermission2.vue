<template>
  <div class="test-permission2">
    <h2>权限2子菜单</h2>
    <el-card style="margin-top: 20px;">
      <template #header>功能测试区域</template>
      <el-space wrap>
        <el-button v-permission="'test:func21'" type="warning">功能21（权限: test:func21）</el-button>
        <el-button v-permission="'test:func22'" type="info">功能22（权限: test:func22）</el-button>
        <el-button v-permission="'test:func23'" type="success">功能23（权限: test:func23）</el-button>
      </el-space>
    </el-card>
  </div>
</template>

<script setup lang="ts">
</script>

<style scoped>
.test-permission2 { padding: 10px; }
</style>