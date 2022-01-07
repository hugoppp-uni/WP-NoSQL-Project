<template>
  <v-container>
    <v-card dir style="margin: auto">

      <v-card-title class="justify-center display-1 font-weight-bold mb-3"
      >Optional Results
      </v-card-title
      >
      <v-card-text class="text-left subtitle-1"
      > WIP: Probably show similar Hashtags results after klicking on a TOP 10 hashtag or embed Hashtags here
      </v-card-text>
    </v-card>
  </v-container>
</template>

<script>


export default {
  name: 'QueryResult',
  components: {
  },

  data: function () {
    return {
    };
  },
  methods: {
    singleWord(value) {
      if (value.length === 0) {
        return true;
      }
      return 'This is required';
    },
  }
}
</script>
