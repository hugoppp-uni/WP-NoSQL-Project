<template>
  <v-container>
<!--    <v-card dir style="margin: auto">-->
      <div class="text-center">
        <v-btn
            class="ma-2"
            outlined
            color="white"
            @click="getTopHashtags"
        >
          Show Trending Hashtags
        </v-btn>
      </div>
<!--Results of Top Hashtags List: -->
<!--    </v-card>-->
    <v-card
        v-if="topHashtagsNotEmpty"
        class="mx-auto"
        max-width="300"
        tile
    >
      <v-list rounded>
<!--        <v-subheader class="ma-2">Top Hashtags</v-subheader>-->
        <v-list-item-group
            v-model="selectedHashtag"
            color="primary"
        >
          <v-list-item
              v-for="(item, index) in topHashtagsResult"
              :key="index"
          >
            <v-list-item-content>
              <v-list-item-title v-text="item.hashtag"></v-list-item-title>
            </v-list-item-content>
          </v-list-item>
        </v-list-item-group>
      </v-list>
    </v-card>

  </v-container>
</template>




<script>
import axios from 'axios'

export default {
  name: 'QueryResult',
  components: {
  },
  computed: {
    topHashtagsNotEmpty: function (){
      return (this.topHashtagsResult.length > 1)
    },

  },


  data: function () {
    return {
      topHashtagsResult: '',
      selectedHashtag: '',
      listDataString: '',
      defaultTopHashtagUrl: 'http://localhost:5038/Hashtag/top/'
    };
  },
  methods: {
    getTopHashtags() {
      axios
          .get(this.createTopHashtagRequestUrl())
          .then(res => {
            this.topHashtagsResult = res.data;
          });

    },
    createTopHashtagRequestUrl(){
      let requestUrl = this.defaultTopHashtagUrl;
      // append selected number of Hashtags:
      requestUrl += '10' //TODO: Replace with input value
      // Append selected language:
      requestUrl += '?language=' + 'en' //TODO: Replace with input value
      return requestUrl;
    }

  }
}
</script>
