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
        <v-list-item-group
            color="primary"
        >
          <v-list-item
              v-for="(item, index) in topHashtagsResult"
              :key="index"
          >
            <v-list-item-content>
              <v-list-item-title @click="publishSelectedHashtag(item.hashtag)" v-text="item.hashtag"></v-list-item-title>
            </v-list-item-content>
          </v-list-item>
        </v-list-item-group>
      </v-list>
    </v-card>

  </v-container>
</template>

<script>
import axios from 'axios'
import eventBus from '../eventBus'

export default {
  mounted() {
    // adding eventBus listener
    eventBus.$on('userInputUpdated', (param) => {
      // console.log('Lang received: ' + param.selectedLanguage)
      this.selectedLanguage = param.selectedLanguage;
      // console.log('LastDays received: ' + param.lastDays)
      this.lastDays = param.lastDays;
      // console.log('TopCount received: ' + param.topCount)
      this.topCount = param.topCount;
    })
  },
  beforeDestroy() {
    // removing eventBus listener
    eventBus.$off('userInputUpdated')
  },
  computed: {
    topHashtagsNotEmpty: function () {
      return (this.topHashtagsResult.length > 1)
    },

  },
  data: function () {
    return {
      //input Data from Settings:
      selectedLanguage: '',
      lastDays: '',
      topCount: '',
      // Data Api Call
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
            console.log('API response received')
          });

    },
    createTopHashtagRequestUrl() {
      let requestUrl = this.defaultTopHashtagUrl;
      // append selected number of Hashtags:
      if (this.topCount >= 1) {
        requestUrl += this.topCount;
      } else {
        requestUrl += '10' // default value
      }
      // Append selected language:
      let languageAppended = false;
      if (this.selectedLanguage !== undefined && this.selectedLanguage !== '' && this.selectedLanguage !== 'al') {
        requestUrl += '?language=' + this.selectedLanguage
        languageAppended = true;
      }
      // Append selected LastDays:
      if (this.lastDays !== undefined && this.lastDays !== '') {
        if (languageAppended) {
          requestUrl += '&';
        } else {
          requestUrl += '?';
        }
        requestUrl += 'lastDays=' + this.lastDays
      }
      console.log("Request URL: " + requestUrl)
      return requestUrl;
    },
    publishSelectedHashtag(selectedHashtag) {
      eventBus.$emit('hashtagSelected', selectedHashtag) // if ChildComponent is mounted, we will have a message in the console
      console.log("Hashtag selected & emitted: " + selectedHashtag);
    }
  }
}
</script>
